using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowzy.Service.Imports;

public sealed partial class ImportService(FlowzyDbContext db, ImportWorkQueue queue,
    IServiceScopeFactory scopes, IConfiguration configuration) : IImportService
{
    private static readonly HashSet<string> ErrorCodes = new("MISSING_REQUIRED_FIELD INVALID_FORMAT DUPLICATE_CODE DUPLICATE_EMAIL INVALID_VALUE UNKNOWN_ERROR UNSUPPORTED_COLUMN DUPLICATED_IN_FILE ALREADY_EXISTS INVALID_EMAIL INVALID_GENDER INVALID_EXPERIENCE INVALID_PHONE INVALID_DATE ACCOUNT_CREATION_FAILED SYSTEM_ERROR LEADER_FALLBACK_WARNING GROUP_SKIPPED MENTOR_ASSIGNMENT_WARNING".Split(' '));
    public async Task<ImportResultResponse> QueueAsync(Stream stream, string filename, long length, string target, string creatorEmail, CancellationToken ct)
    {
        if (length == 0) throw new BadRequestException("File is empty");
        if (string.IsNullOrWhiteSpace(filename)) throw new BadRequestException("Filename is empty");
        var type = Path.GetExtension(filename).ToLowerInvariant() switch { ".csv" => "CSV", ".xlsx" => "XLSX", _ => throw new BadRequestException("Unsupported file type") };
        await queue.Admission.WaitAsync(ct);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // Serialize admission across API instances as well as across HTTP requests.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78362419)", ct);
            if (await db.ImportBatches.AnyAsync(x => x.Status == "QUEUED" || x.Status == "RUNNING", ct)) throw new ConflictException("An import job is already queued or running");
            var creator = await db.Accounts.FirstOrDefaultAsync(x => x.Email.ToLower() == creatorEmail.ToLower(), ct) ?? throw new NotFoundException("Creator account not found");
            var batch = new ImportBatch { TargetType = target, FileName = filename, FileType = type, Status = "QUEUED", StartedAt = DateTime.UtcNow, CreatedBy = creator.Id };
            db.ImportBatches.Add(batch); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            var dir = configuration["Import:UploadDirectory"] ?? Path.Combine(Path.GetTempPath(), "flowzy", "imports");
            string? path = null;
            try
            {
                Directory.CreateDirectory(dir); path = Path.Combine(Path.GetFullPath(dir), $"import-{batch.Id}-{Guid.NewGuid():N}.{type.ToLowerInvariant()}");
                await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) await stream.CopyToAsync(output, ct);
            }
            catch (Exception ex)
            {
                await MarkFailed(batch.Id, "Unable to store import file: " + ex.Message, CancellationToken.None);
                if (path is not null && File.Exists(path)) File.Delete(path);
                throw;
            }
            var response = new ImportResultResponse(batch.Id, target, "QUEUED", filename, type, 0, 0, 0, batch.StartedAt, null);
            queue.Enqueue(new(batch.Id, path)); return response;
        }
        finally { queue.Admission.Release(); }
    }

    public async Task<ImportBatchResponse> GetBatchAsync(long id, CancellationToken ct)
    {
        var b = await db.ImportBatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Batch not found");
        return new(b.Id, b.TargetType, b.FileName, b.FileType, b.Status, b.TotalRows, b.SuccessRows, b.FailedRows, b.StartedAt, b.FinishedAt);
    }
    public async Task<PageResponse<ImportRowErrorResponse>> GetErrorsAsync(long id, int page, int size, string? search,
        int? rowNumber, string? fieldName, string? errorCode, CancellationToken ct)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        if (rowNumber < 1) throw new BadRequestException("Row number must be greater than zero");
        if (!string.IsNullOrEmpty(errorCode) && !ErrorCodes.Contains(errorCode)) throw new BadRequestException("Invalid errorCode value");
        if (!await db.ImportBatches.AnyAsync(x => x.Id == id, ct)) throw new NotFoundException("Batch not found");
        var q = db.ImportRowErrors.AsNoTracking().Where(x => x.BatchId == id);
        if (rowNumber is not null) q = q.Where(x => x.RowNumber == rowNumber);
        if (!string.IsNullOrWhiteSpace(fieldName)) { var f = fieldName.Trim().ToLowerInvariant(); q = q.Where(x => x.FieldName != null && x.FieldName.ToLower() == f); }
        if (!string.IsNullOrEmpty(errorCode)) q = q.Where(x => x.ErrorCode == errorCode);
        if (!string.IsNullOrWhiteSpace(search)) { var p = "%" + search.Trim().ToLowerInvariant() + "%"; q = q.Where(x => EF.Functions.Like(x.FieldName!.ToLower(), p) || EF.Functions.Like(x.ErrorCode.ToLower(), p) || EF.Functions.Like(x.ErrorMessage.ToLower(), p)); }
        var total = await q.LongCountAsync(ct);
        var rows = await q.OrderBy(x => x.RowNumber).ThenBy(x => x.Id).Skip(page * size).Take(size).Select(x => new ImportRowErrorResponse(x.RowNumber, x.FieldName, x.ErrorCode, x.ErrorMessage)).ToListAsync(ct);
        return PageResponse<ImportRowErrorResponse>.Create(rows, page, size, total);
    }

    public async Task FailInterruptedAsync(CancellationToken ct)
    {
        var ids = await db.ImportBatches.Where(x => x.Status == "RUNNING" || x.Status == "QUEUED").Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids) await MarkFailed(id, "Marked failed on backend startup because the previous import worker is no longer running.", ct);
    }

    public async Task ProcessAsync(ImportWorkItem item, CancellationToken ct)
    {
        try
        {
            var batch = await db.ImportBatches.SingleAsync(x => x.Id == item.BatchId, ct);
            batch.Status = "RUNNING"; await db.SaveChangesAsync(ct);
            IReadOnlyList<ImportRow> rows;
            try
            {
                await using var input = File.OpenRead(item.FilePath); rows = ImportFileParser.Parse(input, batch.FileType, batch.TargetType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (ex is BadRequestException && batch.TargetType != "PROBLEM_BANK") throw;
                throw new BadRequestException("Error parsing import file: " + ex.Message);
            }
            if (rows.Count == 0) throw new BadRequestException("File contains no data rows");
            var result = batch.TargetType == "PROBLEM_BANK" ? await ProcessProblems(batch.Id, rows, ct) : await ProcessAccounts(batch.Id, batch.TargetType, rows, ct);
            db.ChangeTracker.Clear(); batch = await db.ImportBatches.SingleAsync(x => x.Id == item.BatchId, ct);
            batch.TotalRows = rows.Count; batch.SuccessRows = result.Success; batch.FailedRows = result.Failed;
            batch.Status = result.Failed == rows.Count ? "FAILED" : "COMPLETED"; batch.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear(); await MarkFailed(item.BatchId, "Import job failed: " + ex.Message, CancellationToken.None);
            if (ex is OperationCanceledException) throw;
        }
        finally { if (File.Exists(item.FilePath)) File.Delete(item.FilePath); }
    }

    private async Task<(int Success, int Failed)> ProcessAccounts(long batchId, string target, IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var roster = target == "STUDENT_ACCOUNT"; var mentor = target == "MENTOR"; var field = roster ? "roll_number" : mentor ? "mentor_code" : "student_code";
        if (!roster && (!rows[0].Data.ContainsKey("email") || !rows[0].Data.ContainsKey("full_name") || !rows[0].Data.ContainsKey(field))) throw new BadRequestException("Invalid headers in the file");
        var emails = rows.Select(x => x.Value("email").ToLowerInvariant()).Where(x => x.Length != 0).ToList();
        var codes = rows.Select(x => x.Value(field).ToLowerInvariant()).Where(x => x.Length != 0).ToList();
        var accounts = await db.Accounts.AsNoTracking().Where(x => emails.Contains(x.Email.ToLower())).ToListAsync(ct);
        var students = mentor ? [] : await db.Students.AsNoTracking().Include(x => x.Account).Where(x => codes.Contains(x.StudentCode.ToLower()) || emails.Contains(x.Account.Email.ToLower())).ToListAsync(ct);
        var existingEmails = accounts.Select(x => x.Email.ToLowerInvariant()).ToHashSet();
        var existingCodes = mentor ? (await db.Mentors.Where(x => codes.Contains(x.MentorCode.ToLower())).Select(x => x.MentorCode.ToLower()).ToListAsync(ct)).ToHashSet() : students.Select(x => x.StudentCode.ToLowerInvariant()).ToHashSet();
        var seenEmails = new HashSet<string>(); var seenCodes = new HashSet<string>();
        var results = new List<(ImportRow Row, IReadOnlyList<ImportIssue> Issues)>(); var success = 0; var failed = 0;
        foreach (var row in rows)
        {
            var email = row.Value("email"); var code = row.Value(field);
            var byEmail = students.FirstOrDefault(x => x.Account.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            var byCode = students.FirstOrDefault(x => x.StudentCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            var reactivate = byEmail is not null && byEmail.Id == byCode?.Id && byEmail.Status == "INACTIVE" && byEmail.Account.Status == "INACTIVE" && (!roster || byEmail.Account.Role == "STUDENT");
            var issues = ImportValidation.AccountRow(row, target, existingEmails, existingCodes, seenEmails, seenCodes, reactivate).ToList();
            if (!roster && byEmail is not null && byCode is not null && byEmail.Id != byCode.Id) issues.Add(new("student_code", "INVALID_VALUE", "Email and student code belong to different existing students"));
            results.Add((row, issues));
            if (issues.Count != 0) { failed++; await SaveIssues(batchId, row, issues, ct); continue; }
            try
            {
                await SaveAccount(row, target, reactivate ? byEmail!.Id : null, ct); success++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++; await SaveIssues(batchId, row, [new(null, "ACCOUNT_CREATION_FAILED", "Database constraint or insert error: " + ex.Message)], ct);
            }
        }
        if (target == "STUDENT") await ProcessGroups(batchId, results, ct);
        return (success, failed);
    }

    private async Task SaveAccount(ImportRow row, string target, long? reactivateId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var store = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        await using var tx = await store.Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow; var roster = target == "STUDENT_ACCOUNT"; var email = row.Value("email").ToLowerInvariant();
        Student? student = null; Account account;
        if (reactivateId is not null)
        {
            student = await store.Students.FromSqlInterpolated($"SELECT * FROM students WHERE id={reactivateId.Value} FOR UPDATE").FirstAsync(ct);
            account = await store.Accounts.SingleAsync(x => x.Id == student.AccountId, ct);
            if (roster && (account.Role != "STUDENT" || !account.Email.Equals(email, StringComparison.OrdinalIgnoreCase) || !student.StudentCode.Equals(row.Value("roll_number"), StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Student identity changed before reactivation: " + reactivateId);
            account.Status = "ACTIVE"; account.MustChangePassword = false; account.UpdatedAt = now;
        }
        else
        {
            var password = roster || row.Value("password").Length == 0 ? SecurePassword() : row.Value("password");
            account = new Account { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = target == "MENTOR" ? "MENTOR" : "STUDENT", Status = "ACTIVE", MustChangePassword = false, CreatedAt = now, UpdatedAt = now };
            store.Accounts.Add(account); await store.SaveChangesAsync(ct);
        }
        if (target == "MENTOR")
        {
            store.Mentors.Add(new Mentor { AccountId = account.Id, MentorCode = row.Value("mentor_code"), FullName = row.Value("full_name"), Phone = Blank(row.Value("phone")), JobTitle = Blank(row.Value("job_title")), Company = Blank(row.Value("company")), Expertise = Blank(row.Value("expertise")), YearsOfExperience = ImportValidation.Experience(row.Value("years_of_experience")), LinkedinUrl = Blank(row.Value("linkedin_url")), Status = "ACTIVE", CreatedAt = now, UpdatedAt = now });
        }
        else
        {
            if (student is null) { student = new Student { AccountId = account.Id, StudentCode = roster ? row.Value("roll_number").ToUpperInvariant() : row.Value("student_code"), CreatedAt = now }; store.Students.Add(student); }
            student.FullName = row.Value("full_name"); student.Status = "ACTIVE"; student.UpdatedAt = now;
            if (roster) { account.Email = email; student.StudentCode = row.Value("roll_number").ToUpperInvariant(); student.ClassName = row.Value("group_name"); }
            else { student.Phone = Blank(row.Value("phone")); student.DateOfBirth = ImportValidation.Date(row.Value("date_of_birth")); student.Gender = Blank(row.Value("gender").ToUpperInvariant()); student.Address = Blank(row.Value("address")); student.Major = Blank(row.Value("major")); student.Cohort = Blank(row.Value("cohort")); student.ClassName = Blank(row.Value("class_name")); }
        }
        await store.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    private async Task SaveIssues(long batchId, ImportRow row, IReadOnlyList<ImportIssue> issues, CancellationToken ct)
    {
        foreach (var issue in issues) db.ImportRowErrors.Add(new ImportRowError { BatchId = batchId, RowNumber = row.Number, FieldName = issue.Field, ErrorCode = issue.Code, ErrorMessage = issue.Message, RawData = JsonSerializer.Serialize(row.Data) });
        await db.SaveChangesAsync(ct);
    }
    private async Task MarkFailed(long id, string message, CancellationToken ct)
    {
        var batch = await db.ImportBatches.SingleAsync(x => x.Id == id, ct); batch.Status = "FAILED"; batch.FinishedAt = DateTime.UtcNow;
        db.ImportRowErrors.Add(new ImportRowError { BatchId = id, RowNumber = 1, ErrorCode = "SYSTEM_ERROR", ErrorMessage = message }); await db.SaveChangesAsync(ct);
    }
    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string SecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", lower = "abcdefghijklmnopqrstuvwxyz", digits = "0123456789", symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        var all = upper + lower + digits + symbols; var chars = new char[12]; var sets = new[] { upper, lower, digits, symbols };
        for (var i = 0; i < chars.Length; i++) { var set = i < 4 ? sets[i] : all; chars[i] = set[RandomNumberGenerator.GetInt32(set.Length)]; }
        RandomNumberGenerator.Shuffle(chars.AsSpan()); return new string(chars);
    }
}
