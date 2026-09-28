using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Service.PortedDomains;

public sealed class PortedDomainService(FlowzyDbContext db) : IPortedDomainService
{
    private static DateTime Now => DateTime.UtcNow;

    public async Task<ProblemDomainResponse> CreateProblemDomainAsync(ProblemDomainRequest r, CancellationToken ct)
    {
        var code = Required(r.Code, "Problem domain code is required").ToUpperInvariant();
        if (await db.ProblemDomains.AnyAsync(x => x.Code.ToUpper() == code, ct))
            throw new ConflictException($"Problem domain code already exists: {code}");
        var now = Now;
        var e = new ProblemDomain { Code = code, Name = Required(r.Name, "Domain name is required"), Description = r.Description,
            MacroDomain = First(r.MacroDomain, r.Name), SubDomain = First(r.SubDomain, r.Description),
            TypicalExamples = Blank(r.TypicalExamples), PrimaryDiscipline = Blank(r.PrimaryDiscipline),
            SupportingDisciplines = Blank(r.SupportingDisciplines), BestSources = Blank(r.BestSources),
            StudentCapabilities = Blank(r.StudentCapabilities), PotentialOutputs = Blank(r.PotentialOutputs), Notes = Blank(r.Notes),
            Status = Upper(r.Status ?? "ACTIVE"), CreatedAt = now, UpdatedAt = now };
        db.ProblemDomains.Add(e); await db.SaveChangesAsync(ct); return Domain(e);
    }

    public async Task<ProblemDomainResponse> UpdateProblemDomainAsync(long id, UpdateProblemDomainRequest r, CancellationToken ct)
    {
        var e = await db.ProblemDomains.FindAsync([id], ct) ?? throw new NotFoundException($"Problem domain not found with ID: {id}");
        if (r.Code is not null) { var code = Required(r.Code, "Problem domain code is required").ToUpperInvariant();
            if (!code.Equals(e.Code, StringComparison.OrdinalIgnoreCase) && await db.ProblemDomains.AnyAsync(x => x.Code.ToUpper() == code, ct))
                throw new ConflictException($"Problem domain code already exists: {code}"); e.Code = code; }
        if (r.Name is not null) e.Name = r.Name; if (r.Description is not null) e.Description = r.Description;
        if (r.MacroDomain is not null) e.MacroDomain = r.MacroDomain; if (r.SubDomain is not null) e.SubDomain = r.SubDomain;
        if (r.TypicalExamples is not null) e.TypicalExamples = r.TypicalExamples; if (r.PrimaryDiscipline is not null) e.PrimaryDiscipline = r.PrimaryDiscipline;
        if (r.SupportingDisciplines is not null) e.SupportingDisciplines = r.SupportingDisciplines; if (r.BestSources is not null) e.BestSources = r.BestSources;
        if (r.StudentCapabilities is not null) e.StudentCapabilities = r.StudentCapabilities; if (r.PotentialOutputs is not null) e.PotentialOutputs = r.PotentialOutputs;
        if (r.Notes is not null) e.Notes = r.Notes; if (r.Status is not null) e.Status = Upper(r.Status); e.UpdatedAt = Now;
        await db.SaveChangesAsync(ct); return Domain(e);
    }

    public async Task<ProblemDetailResponse> CreateProblemAsync(ProblemWriteRequest r, CancellationToken ct)
    {
        var domain = await ActiveDomain(r.DomainCode, ct);
        if (!string.IsNullOrWhiteSpace(r.Code) && await db.Problems.AnyAsync(x => x.Code == r.Code, ct))
            throw new BadRequestException($"Problem code already exists: {r.Code}");
        var now = Now; var e = new Problem { Domain = domain, Title = Required(r.Title, "Title is required"), Statement = Required(r.Statement, "Statement is required"),
            Code = Blank(r.Code), StrategicTheme = r.StrategicTheme, ResearchArea = r.ResearchArea, DifficultyLevel = Upper(r.DifficultyLevel),
            ExpectedOutput = r.ExpectedOutput, OwnerLab = r.OwnerLab, SuggestedCourses = r.SuggestedCourses, DriveFolderLink = r.DriveFolderLink,
            SourceType = "OFFICIAL", Status = Upper(r.Status ?? "ACTIVE"), CreatedAt = now, UpdatedAt = now };
        db.Problems.Add(e); await db.SaveChangesAsync(ct); return await Problem(e.Id, ct);
    }

    public async Task<ProblemDetailResponse> UpdateProblemAsync(long id, ProblemPatchRequest r, CancellationToken ct)
    {
        var e = await db.Problems.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException($"Problem not found with ID: {id}");
        if (r.DomainCode is not null)
        {
            if (string.IsNullOrWhiteSpace(r.DomainCode)) { e.Domain = null; e.DomainId = null; }
            else if (e.Domain is null || r.DomainCode != e.Domain.Code) e.Domain = await ActiveDomain(r.DomainCode, ct);
        }
        if (r.Code is not null && r.Code != e.Code) { if (await db.Problems.AnyAsync(x => x.Code == r.Code, ct)) throw new BadRequestException($"Problem code already exists: {r.Code}"); e.Code = r.Code; }
        if (r.Title is not null) e.Title = r.Title; if (r.Statement is not null) e.Statement = r.Statement;
        if (r.StrategicTheme is not null) e.StrategicTheme = r.StrategicTheme; if (r.ResearchArea is not null) e.ResearchArea = r.ResearchArea;
        if (r.DifficultyLevel is not null) e.DifficultyLevel = Upper(r.DifficultyLevel); if (r.ExpectedOutput is not null) e.ExpectedOutput = r.ExpectedOutput;
        if (r.OwnerLab is not null) e.OwnerLab = r.OwnerLab; if (r.SuggestedCourses is not null) e.SuggestedCourses = r.SuggestedCourses;
        if (r.DriveFolderLink is not null) e.DriveFolderLink = r.DriveFolderLink; if (r.Status is not null) e.Status = Upper(r.Status);
        e.UpdatedAt = Now; await db.SaveChangesAsync(ct); return await Problem(id, ct);
    }

    public async Task<ProblemDetailResponse> SetProblemStatusAsync(long id, string status, CancellationToken ct)
    { var e = await db.Problems.FindAsync([id], ct) ?? throw new NotFoundException($"Problem not found with ID: {id}"); e.Status = Upper(status); e.UpdatedAt = Now; await db.SaveChangesAsync(ct); return await Problem(id, ct); }

    public async Task<ProblemDetailResponse> ReviewProblemAsync(long id, ReviewProblemRequest r, string email, CancellationToken ct)
    {
        var account = await Account(email, ct); if (account.Role != "ADMIN") throw new ForbiddenException("Only admin can review proposals");
        var e = await db.Problems.Include(x => x.ProposedByGroup).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException($"Problem not found with ID: {id}");
        if (e.SourceType != "SELF_PROPOSED") throw new BadRequestException("Only self-proposed problems can be reviewed");
        if (e.Status != "PENDING_REVIEW") throw new BadRequestException("Problem is not in PENDING_REVIEW status");
        var status = Upper(r.Status); if (status is not ("APPROVED" or "REJECTED")) throw new BadRequestException("Review status must be APPROVED or REJECTED");
        if (status == "REJECTED" && string.IsNullOrWhiteSpace(r.Comment)) throw new BadRequestException("Comment is required when rejecting a proposal");
        if (status == "APPROVED") { e.SourceType = "OFFICIAL"; e.Status = "ACTIVE"; } else { e.Status = "REJECTED"; if (e.ProposedByGroup?.SelectedProblemId == id) e.ProposedByGroup.SelectedProblemId = null; }
        e.ReviewComment = r.Comment; e.ReviewedByAccountId = account.Id; e.ReviewedAt = Now; e.UpdatedAt = Now;
        await db.SaveChangesAsync(ct); return await Problem(id, ct);
    }


    private async Task<Account> Account(string email,CancellationToken ct)=>await db.Accounts.FirstOrDefaultAsync(x=>x.Email.ToLower()==email.ToLower(),ct)??throw new UnauthorizedException("User not found");
    private async Task<ProblemDomain?> ActiveDomain(string? code,CancellationToken ct){if(string.IsNullOrWhiteSpace(code))return null;var d=await db.ProblemDomains.FirstOrDefaultAsync(x=>x.Code==code,ct)??throw new BadRequestException($"Problem domain not found with code: {code}");if(d.Status!="ACTIVE")throw new BadRequestException("Cannot assign problem to an INACTIVE domain");return d;}
    private async Task<ProblemDetailResponse> Problem(long id,CancellationToken ct){var e=await db.Problems.AsNoTracking().Include(x=>x.Domain).Include(x=>x.ProposedByGroup).Include(x=>x.ProposedByStudent).Include(x=>x.ReviewedByAccount).FirstAsync(x=>x.Id==id,ct);return new(e.Id,e.Code,e.Title,e.Statement,e.StrategicTheme,e.ResearchArea,e.DifficultyLevel,e.ExpectedOutput,e.OwnerLab,e.SuggestedCourses,e.DriveFolderLink,e.SourceType,e.Status,e.Domain is null?null:new DomainInfo(e.Domain.Id,e.Domain.Code,e.Domain.Name),e.ProposedByGroup is null?null:new GroupInfo(e.ProposedByGroup.Id,e.ProposedByGroup.GroupNo,e.ProposedByGroup.Name),e.ProposedByStudent is null?null:new StudentInfo(e.ProposedByStudent.Id,e.ProposedByStudent.StudentCode,e.ProposedByStudent.FullName),e.ReviewComment,e.ReviewedByAccount is null?null:new ReviewerInfo(e.ReviewedByAccount.Id,e.ReviewedByAccount.Email),e.ReviewedAt,e.CreatedAt,e.UpdatedAt);}
    private static ProblemDomainResponse Domain(ProblemDomain e)=>new(e.Id,e.Code,e.Name,e.Description,e.MacroDomain,e.SubDomain,e.TypicalExamples,e.PrimaryDiscipline,e.SupportingDisciplines,e.BestSources,e.StudentCapabilities,e.PotentialOutputs,e.Notes,e.Status,e.CreatedAt,e.UpdatedAt);
    private static string Required(string? v,string message)=>string.IsNullOrWhiteSpace(v)?throw new BadRequestException(message):v.Trim(); private static string Upper(string v)=>v.Trim().ToUpperInvariant(); private static string? Blank(string? v)=>string.IsNullOrWhiteSpace(v)?null:v; private static string? First(string? a,string? b)=>Blank(a)??Blank(b);
}
