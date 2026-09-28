using Flowzy.Service.Authentication;
using Flowzy.Service.Admin;
using Flowzy.Service.Options;
using Flowzy.Service.Platform;
using Flowzy.Service.Grades;
using Flowzy.Service.Problems;
using Flowzy.Service.Startup;
using Flowzy.Service.Students;
using Flowzy.Service.Submissions;
using Flowzy.Service.Mentors;
using Flowzy.Service.Feedback;
using Flowzy.Service.Groups;
using Flowzy.Service.PortedDomains;
using Flowzy.Service.Dashboard;
using Flowzy.Service.Backup;
using Flowzy.Service.Tasks;
using Flowzy.Service.Imports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowzy.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddFlowzyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddHttpClient<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddScoped<AdminSeedService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IMilestoneGradeService, MilestoneGradeService>();
        services.AddScoped<IGradeMatrixService, GradeMatrixService>();
        services.AddScoped<IInstructorProblemService, InstructorProblemService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IInstructorSubmissionService, InstructorSubmissionService>();
        services.AddScoped<IMilestoneSubmissionService, MilestoneSubmissionService>();
        services.AddScoped<Flowzy.Service.Terms.IAcademicTermService, Flowzy.Service.Terms.AcademicTermService>();
        services.AddScoped<IMentorAvailabilityService, MentorAvailabilityService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IMentorMeetingReportService, MentorMeetingReportService>();
        services.AddScoped<IGroupMeetingService, GroupMeetingService>();
        services.AddScoped<IAdminGroupService, AdminGroupService>();
        services.AddScoped<IGroupOperationsService, GroupOperationsService>();
        services.AddScoped<IGroupMembershipService, GroupMembershipService>();
        services.AddScoped<IGroupProblemService, GroupProblemService>();
        services.AddScoped<IInstructorBoardService, InstructorBoardService>();
        services.AddScoped<Flowzy.Service.Timelines.ICourseMilestoneService, Flowzy.Service.Timelines.CourseMilestoneService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<BackupQueue>();
        services.AddSingleton<BackupOperationGate>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPostgresBackupProcess, PostgresBackupProcess>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IGroupTaskService, GroupTaskService>();
        services.AddScoped<ITaskBoardService, TaskBoardService>();
        services.AddSingleton<ImportWorkQueue>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IAdminFeedbackService, AdminFeedbackService>();
        services.AddScoped<IPortedDomainService, PortedDomainService>();
        return services;
    }
}
