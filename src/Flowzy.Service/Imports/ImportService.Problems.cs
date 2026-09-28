using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowzy.Service.Imports;

public sealed partial class ImportService
{
    private async Task<(int Success, int Failed)> ProcessProblems(long batchId, IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var success = 0; var failed = 0; var domains = new HashSet<string>(); var problems = new HashSet<string>();
        foreach (var row in rows.OrderBy(x => x.Type == "domain" ? 0 : 1))
        {
            var domain = row.Type == "domain";
            var issues = ImportValidation.ProblemRow(row, domain ? domains : problems);
            if (issues.Count != 0) { failed++; await SaveIssues(batchId, row, issues, ct); continue; }
            try
            {
                await using var scope = scopes.CreateAsyncScope(); var store = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
                await using var tx = await store.Database.BeginTransactionAsync(ct);
                var code = row.Value("code"); var now = DateTime.UtcNow;
                if (domain)
                {
                    var e = await store.ProblemDomains.FirstOrDefaultAsync(x => x.Code == code, ct);
                    if (e is null) { e = new ProblemDomain { Code = code, CreatedAt = now }; store.ProblemDomains.Add(e); }
                    e.Name = row.Value("name"); e.Description = Blank(row.Value("description"));
                    e.MacroDomain = Blank(row.Value("macro_domain")) ?? Blank(e.Name); e.SubDomain = Blank(row.Value("sub_domain")) ?? e.Description;
                    e.TypicalExamples = Blank(row.Value("typical_examples")); e.PrimaryDiscipline = Blank(row.Value("primary_discipline"));
                    e.SupportingDisciplines = Blank(row.Value("supporting_disciplines")); e.BestSources = Blank(row.Value("best_sources"));
                    e.StudentCapabilities = Blank(row.Value("student_capabilities")); e.PotentialOutputs = Blank(row.Value("potential_outputs"));
                    e.Notes = Blank(row.Value("notes")); e.Status = row.Value("status").Equals("inactive", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE"; e.UpdatedAt = now;
                }
                else
                {
                    var e = await store.Problems.FirstOrDefaultAsync(x => x.Code == code, ct);
                    if (e is null) { e = new Problem { Code = code, CreatedAt = now }; store.Problems.Add(e); }
                    var domainCode = row.Value("domain_code");
                    e.DomainId = domainCode.Length == 0 ? null : await store.ProblemDomains.Where(x => x.Code == domainCode).Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
                    e.Title = row.Value("title"); e.Statement = row.Value("statement"); e.StrategicTheme = Blank(row.Value("strategic_theme"));
                    e.ResearchArea = Blank(row.Value("research_area")); e.ExpectedOutput = Blank(row.Value("expected_output"));
                    e.OwnerLab = Blank(row.Value("owner_lab")); e.SuggestedCourses = Blank(row.Value("suggested_courses")); e.DriveFolderLink = Blank(row.Value("drive_folder_link"));
                    e.DifficultyLevel = ImportValidation.Difficulty(row.Value("difficulty_level"))!; e.SourceType = "OFFICIAL";
                    e.Status = row.Value("status").Equals("inactive", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE"; e.UpdatedAt = now;
                }
                await store.SaveChangesAsync(ct); await tx.CommitAsync(ct); success++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { failed++; await SaveIssues(batchId, row, [new(null, "SYSTEM_ERROR", "Database error saving " + (domain ? "domain: " : "problem: ") + ex.Message)], ct); }
        }
        return (success, failed);
    }
}
