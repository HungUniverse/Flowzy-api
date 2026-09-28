using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Feedback;

public sealed class FeedbackService(IAccountRepository accounts, IFeedbackRepository feedbacks) : IFeedbackService
{
    public async Task<IReadOnlyList<TermFeedbackResponse>> GetOwnAsync(string? term, FeedbackStatus? status,
        string currentUserEmail, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(currentUserEmail, cancellationToken);
        var student = account?.Student
            ?? throw new ForbiddenException($"Student profile not found for user: {currentUserEmail}");
        var result = await feedbacks.FindOwnAsync(student.Id, Normalize(term), status?.ToString(), cancellationToken);
        return result.Select(Map).ToList();
    }

    public async Task<TermFeedbackResponse> SubmitAsync(long id, SubmitFeedbackRequest request,
        string currentUserEmail, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(currentUserEmail, cancellationToken);
        var student = account?.Student
            ?? throw new ForbiddenException($"Student profile not found for user: {currentUserEmail}");
        var feedback = await feedbacks.FindAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Feedback record not found with id: {id}");
        if (feedback.StudentId != student.Id)
            throw new ForbiddenException("You are not allowed to submit or update feedback for another student.");
        if (feedback.AcademicTerm.Status == "OPEN")
            throw new ConflictException("Feedback submission is not allowed while the academic term is OPEN.");

        var comment = request.Comment?.Trim();
        feedback.Rating = request.Rating;
        feedback.Comment = string.IsNullOrEmpty(comment) ? null : comment;
        feedback.Status = "SUBMITTED";
        feedback.SubmittedAt ??= DateTime.UtcNow;
        feedback.Version++;
        await feedbacks.SaveAsync(cancellationToken);
        return Map(feedback);
    }

    public async Task<FeedbackReceivedSummary> GetReceivedAsync(string email, string? term, string? courseCode,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken)
            ?? throw new ForbiddenException($"User account not found: {email}");
        long targetId;
        string targetCode;
        string targetName;
        string targetType;
        long? mentorId = null;
        long? instructorId = null;
        if (account.Role == "MENTOR")
        {
            var mentor = account.Mentor ?? throw new ForbiddenException($"Mentor profile not found for user: {email}");
            mentorId = targetId = mentor.Id;
            targetCode = mentor.MentorCode;
            targetName = mentor.FullName;
            targetType = "MENTOR";
        }
        else if (account.Role == "INSTRUCTOR")
        {
            var instructor = account.Instructor
                ?? throw new ForbiddenException($"Instructor profile not found for user: {email}");
            instructorId = targetId = instructor.Id;
            targetCode = instructor.InstructorCode;
            targetName = instructor.FullName;
            targetType = "INSTRUCTOR";
        }
        else
        {
            throw new ForbiddenException("Only mentors or instructors can view received feedbacks.");
        }

        var normalizedTerm = Normalize(term);
        var normalizedCourse = Normalize(courseCode);
        var result = await feedbacks.FindReceivedAsync(mentorId, instructorId, normalizedTerm, normalizedCourse,
            cancellationToken);
        var average = result.Count == 0 ? 0d : Math.Round(result.Average(x => x.Rating!.Value), 2);
        var distribution = Enumerable.Range(1, 5).ToDictionary(rating => rating,
            rating => (long)result.Count(x => x.Rating == rating));
        var entries = result.Select(x => new ReceivedFeedbackEntry(x.Id, x.Rating, x.Comment, x.SubmittedAt,
            x.AcademicTerm?.Code, x.Group?.CourseCode, x.GroupId, x.Group?.Name)).ToList();
        return new FeedbackReceivedSummary(targetId, targetCode, targetName, targetType, normalizedTerm,
            normalizedCourse, result.Count, average, distribution, entries);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : value.Trim().ToUpperInvariant();

    private static TermFeedbackResponse Map(TermFeedback feedback) => new(feedback.Id, feedback.AcademicTermId,
        feedback.AcademicTerm.Code, feedback.AcademicTerm.Status, feedback.GroupId, feedback.Group.Name,
        feedback.StudentId, feedback.Student.FullName, feedback.Student.StudentCode, feedback.TargetType,
        feedback.MentorId, feedback.Mentor?.FullName, feedback.InstructorId, feedback.Instructor?.FullName,
        feedback.Rating, feedback.Comment, feedback.Status, feedback.SubmittedAt, feedback.Version);
}
