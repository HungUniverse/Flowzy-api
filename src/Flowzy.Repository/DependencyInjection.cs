using Flowzy.Repository.Data;
using Flowzy.Repository.Migrations;
using Flowzy.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowzy.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddFlowzyRepository(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FlowzyDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
        services.AddScoped<IDatabaseStatusRepository, DatabaseStatusRepository>();
        services.AddScoped<IPlatformReadRepository, PlatformReadRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IMilestoneGradeRepository, MilestoneGradeRepository>();
        services.AddScoped<IProblemReviewRepository, ProblemReviewRepository>();
        services.AddScoped<IStudentDiscoveryRepository, StudentDiscoveryRepository>();
        services.AddScoped<IInstructorSubmissionRepository, InstructorSubmissionRepository>();
        services.AddScoped<IMilestoneSubmissionRepository, MilestoneSubmissionRepository>();
        services.AddScoped<IAcademicTermRepository, AcademicTermRepository>();
        services.AddScoped<IGroupMeetingRepository, GroupMeetingRepository>();
        services.AddScoped<IInstructorBoardRepository, InstructorBoardRepository>();
        services.AddScoped<ICourseMilestoneRepository, CourseMilestoneRepository>();
        services.AddScoped<IGradeMatrixRepository, GradeMatrixRepository>();
        services.AddScoped<IMentorAvailabilityRepository, MentorAvailabilityRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IMentorMeetingReportRepository, MentorMeetingReportRepository>();
        services.AddScoped<IAdminGroupRepository, AdminGroupRepository>();
        services.AddScoped<IGroupMembershipRepository, GroupMembershipRepository>();
        services.AddScoped<IGroupProblemRepository, GroupProblemRepository>();
        services.AddScoped<ITaskBoardRepository, TaskBoardRepository>();
        services.AddScoped<IBackupRepository, BackupRepository>();
        services.AddSingleton(provider => new SchemaMigrationRunner(
            connectionString,
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SchemaMigrationRunner>>()));
        return services;
    }
}
