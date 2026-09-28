using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public interface IGroupProblemRepository
{
    Task<IDbContextTransaction> BeginAsync(CancellationToken ct);
    Task<StudentGroup?> GroupAsync(long id, bool forUpdate, CancellationToken ct);
    Task<Student?> StudentAsync(string email, CancellationToken ct);
    Task<bool> IsMemberAsync(long group, long student, CancellationToken ct);
    Task<Problem?> ProblemAsync(long id, CancellationToken ct);
    Task<ProblemDomain?> DomainAsync(string code, CancellationToken ct);
    Task<List<Problem>> ProposalsAsync(long group, CancellationToken ct);
    void Add(Problem problem);
    void Remove(Problem problem);
    Task SaveAsync(CancellationToken ct);
}
