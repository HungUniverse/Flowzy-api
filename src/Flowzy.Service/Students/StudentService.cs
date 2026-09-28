using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Students;

public sealed class StudentService(IAccountRepository accounts, IStudentDiscoveryRepository students) : IStudentService
{
    public async Task<StudentProfileResponse> GetByIdAsync(long id, string currentUserEmail,
        CancellationToken cancellationToken = default)
    {
        await RequireStudentAsync(currentUserEmail, cancellationToken);
        var student = await students.FindActiveByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Student not found with id: {id}");
        return Map(student);
    }

    public async Task<PageResponse<StudentProfileResponse>> GetUngroupedAsync(string? term, string? courseCode,
        string? search, int page, int size, string currentUserEmail, CancellationToken cancellationToken = default)
    {
        await RequireStudentAsync(currentUserEmail, cancellationToken);
        if (string.IsNullOrWhiteSpace(term)) throw new BadRequestException("term is required");
        if (string.IsNullOrWhiteSpace(courseCode)) throw new BadRequestException("courseCode is required");
        if (page < 0) throw new BadRequestException("Page must be zero or greater");
        if (size <= 0) throw new BadRequestException("Size must be greater than zero");

        var result = await students.FindUngroupedAsync(term, courseCode, search, page, size, cancellationToken);
        return PageResponse<StudentProfileResponse>.Create(result.Items.Select(Map).ToList(), page, size, result.Total);
    }

    private async Task RequireStudentAsync(string email, CancellationToken cancellationToken)
    {
        var caller = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedException("User account not found");
        if (caller.Role != "STUDENT") throw new ForbiddenException("Access denied");
    }

    private static StudentProfileResponse Map(Student student) => new(student.Id, student.StudentCode,
        student.FullName, student.Account?.Email!, student.Phone, student.DateOfBirth, student.Gender,
        student.Address, student.Major, student.Cohort, student.ClassName, student.Status);
}
