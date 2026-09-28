using System.Globalization;
using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Service.Terms;

public sealed class AcademicTermService(IAcademicTermRepository repository, IRefreshTokenRepository tokens, TimeProvider clock) : IAcademicTermService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public async Task<PageResponse<AcademicTermResponse>> ListTermsAsync(int page, int size, CancellationToken ct)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var result = await repository.PageAsync(page, size, ct);
        var rows = new List<AcademicTermResponse>();
        foreach (var term in result.Items) rows.Add(await Map(term, ct));
        return PageResponse<AcademicTermResponse>.Create(rows, page, size, result.Total);
    }

    public async Task<AcademicTermResponse> CreateTermAsync(CreateAcademicTermRequest request, CancellationToken ct)
    {
        var code = Normalize(request.Code);
        await using var tx = await repository.BeginAsync(ct);
        var existing = await repository.FindAsync(code, false, ct);
        if (existing is not null)
        {
            if (existing.Status != "OPEN") throw new ConflictException("Academic term is already closed: " + code);
            var found = await Map(existing, ct); await tx.CommitAsync(ct); return found;
        }
        var open = await repository.LatestOpenAsync(ct);
        if (open is not null) throw new ConflictException($"Close academic term {open.Code} before creating a new term");
        var term = new AcademicTerm { Code = code, Status = "OPEN", CreatedAt = Now, UpdatedAt = Now };
        repository.Add(term);
        try { await repository.SaveAsync(ct); }
        catch (DbUpdateException) { throw new ConflictException("Another academic term is already open"); }
        var result = await Map(term, ct); await tx.CommitAsync(ct); return result;
    }

    public async Task<AcademicTermResponse> CloseTermAsync(string termCode, string email, CancellationToken ct)
    {
        var code = Normalize(termCode);
        await using var tx = await repository.BeginAsync(ct);
        var term = await repository.FindAsync(code, false, ct);
        if (term is null)
        {
            if (!await repository.HasGroupsAsync(code, ct)) throw Missing(code);
            term = new AcademicTerm { Code = code, Status = "OPEN", CreatedAt = Now, UpdatedAt = Now };
            repository.Add(term); await repository.SaveAsync(ct);
        }
        if (term.Status != "CLOSED")
        {
            // Same lock order as Java close: every group in id order, then the term.
            var groups = await repository.LockGroupsAsync(code, ct);
            term = await repository.FindAsync(code, true, ct) ?? throw Missing(code);
            if (term.Status != "CLOSED")
            {
                var admin = await repository.AccountAsync(email, ct) ?? throw new NotFoundException("Admin account not found with email: " + email);
                var now = Now; term.Status = "CLOSED"; term.ClosedAt = now; term.ClosedByAccountId = admin.Id; term.ClosedByAccount = admin; term.UpdatedAt = now;
                await repository.SaveAsync(ct);
                await repository.CancelPendingAsync(code, now, ct);
                foreach (var group in groups)
                foreach (var member in group.StudentGroupMembers)
                {
                    var created = false;
                    foreach (var target in new[] { "MENTOR", "INSTRUCTOR" })
                    {
                        var targetId = target == "MENTOR" ? group.MentorId : group.InstructorId;
                        if (targetId is null || await repository.HasFeedbackAsync(term.Id, group.Id, member.StudentId, target, ct)) continue;
                        repository.Add(new TermFeedback { AcademicTermId = term.Id, GroupId = group.Id, StudentId = member.StudentId,
                            TargetType = target, MentorId = target == "MENTOR" ? targetId : null, InstructorId = target == "INSTRUCTOR" ? targetId : null,
                            Status = "PENDING", Version = 0, CreatedAt = now, UpdatedAt = now });
                        created = true;
                    }
                    if (created)
                    {
                        var id = term.Id.ToString(CultureInfo.InvariantCulture);
                        await repository.AddNotificationAsync(new Notification { RecipientId = member.Student.AccountId,
                            Type = "TERM_FEEDBACK_AVAILABLE", Title = "Feedback Forms Generated",
                            Body = $"Please submit your feedback for the closed term {term.Code}.", ActionKey = "OPEN_FEEDBACK",
                            ActionParams = JsonSerializer.Serialize(new Dictionary<string, string> { ["termId"] = id, ["termCode"] = term.Code }),
                            EntityType = "AcademicTerm", EntityId = id, EventKey = $"TERM_FEEDBACK_AVAILABLE:{id}:{member.StudentId}", CreatedAt = now, UpdatedAt = now }, ct);
                    }
                }
                await repository.SaveAsync(ct);
            }
        }
        var result = await Map(term, ct); await tx.CommitAsync(ct); return result;
    }

    public async Task<ArchiveTermStudentsResponse> ArchiveStudentsAsync(string termCode, CancellationToken ct)
    {
        var code = Normalize(termCode);
        await using var tx = await repository.BeginAsync(ct);
        var term = await repository.FindAsync(code, true, ct) ?? throw Missing(code);
        if (term.Status != "CLOSED") throw new BadRequestException("Academic term must be closed before students can be archived");
        foreach (var group in await repository.LockGroupsAsync(code, ct)) { group.Status = "INACTIVE"; group.UpdatedAt = Now; }
        var students = await repository.StudentsAsync(code, ct);
        var otherOpen = await repository.StudentsInOtherOpenTermsAsync(code, ct);
        long archived = 0, skipped = 0, inactive = 0;
        foreach (var student in students)
        {
            var account = student.Account;
            if (otherOpen.Contains(student.Id))
            {
                student.Status = "ACTIVE"; account.Status = "ACTIVE"; student.UpdatedAt = Now; account.UpdatedAt = Now; skipped++; continue;
            }
            if (student.Status == "INACTIVE" && account.Status == "INACTIVE") { inactive++; continue; }
            student.Status = "INACTIVE"; account.Status = "INACTIVE"; student.UpdatedAt = Now; account.UpdatedAt = Now;
            await tokens.DeleteForAccountAsync(account.Id, ct); archived++;
        }
        await repository.SaveAsync(ct); await tx.CommitAsync(ct);
        return new(inactive, archived, skipped, 0, code);
    }

    public async Task DeleteTermAsync(string termCode, CancellationToken ct)
    {
        var code = Normalize(termCode);
        await using var tx = await repository.BeginAsync(ct);
        var term = await repository.FindAsync(code, true, ct) ?? throw Missing(code);
        var counts = await repository.CountsAsync(term, ct);
        if (counts.Groups != 0 || counts.Expected != 0) throw new ConflictException("Academic term cannot be deleted because it already has group or feedback history");
        repository.Remove(term); await repository.SaveAsync(ct); await tx.CommitAsync(ct);
    }

    private async Task<AcademicTermResponse> Map(AcademicTerm term, CancellationToken ct)
    {
        var counts = await repository.CountsAsync(term, ct);
        return new(term.Id, term.Code, term.Status, term.ClosedAt, term.ClosedByAccount?.Email, counts.Groups, counts.Expected, counts.Submitted);
    }
    private static NotFoundException Missing(string code) => new("Academic term not found with code: " + code);
    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new BadRequestException("Academic term code is required");
        var code = value.Trim().ToUpperInvariant();
        if (code.Length > 30) throw new BadRequestException("Academic term code must be at most 30 characters");
        return code;
    }
}
