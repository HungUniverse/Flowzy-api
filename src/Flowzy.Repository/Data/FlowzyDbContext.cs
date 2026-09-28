using System;
using System.Collections.Generic;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Data;

public partial class FlowzyDbContext : DbContext
{
    public FlowzyDbContext(DbContextOptions<FlowzyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicTerm> AcademicTerms { get; set; }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<BackupJob> BackupJobs { get; set; }

    public virtual DbSet<BackupScheduleSetting> BackupScheduleSettings { get; set; }

    public virtual DbSet<CourseMilestone> CourseMilestones { get; set; }

    public virtual DbSet<GroupInvitation> GroupInvitations { get; set; }

    public virtual DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }

    public virtual DbSet<GroupRecruitmentNeed> GroupRecruitmentNeeds { get; set; }

    public virtual DbSet<GroupTask> GroupTasks { get; set; }

    public virtual DbSet<ImportBatch> ImportBatches { get; set; }

    public virtual DbSet<ImportRowError> ImportRowErrors { get; set; }

    public virtual DbSet<Instructor> Instructors { get; set; }

    public virtual DbSet<Mentor> Mentors { get; set; }

    public virtual DbSet<MentorAvailabilitySlot> MentorAvailabilitySlots { get; set; }

    public virtual DbSet<MentorMeeting> MentorMeetings { get; set; }

    public virtual DbSet<MilestoneContributionAgreement> MilestoneContributionAgreements { get; set; }

    public virtual DbSet<MilestoneContributionRevision> MilestoneContributionRevisions { get; set; }

    public virtual DbSet<MilestoneGrade> MilestoneGrades { get; set; }

    public virtual DbSet<MilestoneGroupGrade> MilestoneGroupGrades { get; set; }

    public virtual DbSet<MilestoneMemberScore> MilestoneMemberScores { get; set; }

    public virtual DbSet<MilestoneOutcome> MilestoneOutcomes { get; set; }

    public virtual DbSet<MilestoneSubmission> MilestoneSubmissions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Problem> Problems { get; set; }

    public virtual DbSet<ProblemDomain> ProblemDomains { get; set; }

    public virtual DbSet<ProblemEvaluationCriterion> ProblemEvaluationCriteria { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentGroup> StudentGroups { get; set; }

    public virtual DbSet<StudentGroupMember> StudentGroupMembers { get; set; }

    public virtual DbSet<TaskActivity> TaskActivities { get; set; }

    public virtual DbSet<TaskAssignee> TaskAssignees { get; set; }

    public virtual DbSet<TaskBoard> TaskBoards { get; set; }

    public virtual DbSet<TaskChecklistItem> TaskChecklistItems { get; set; }

    public virtual DbSet<TaskComment> TaskComments { get; set; }

    public virtual DbSet<TermFeedback> TermFeedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicTerm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("academic_terms_pkey");

            entity.HasIndex(e => e.Status, "uq_academic_terms_single_open")
                .IsUnique()
                .HasFilter("((status)::text = 'OPEN'::text)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'OPEN'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ClosedByAccount).WithMany(p => p.AcademicTerms)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_academic_terms_closed_by");
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true);
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<BackupJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("backup_jobs_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.RequestedByAccount).WithMany(p => p.BackupJobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_backup_jobs_requested_by");
        });

        modelBuilder.Entity<BackupScheduleSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("backup_schedule_settings_pkey");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BackupDir).HasDefaultValueSql("'/var/backups/fspark/postgres'::character varying");
            entity.Property(e => e.CronExpression).HasDefaultValueSql("'0 0 2 * * *'::character varying");
            entity.Property(e => e.Enabled).HasDefaultValue(false);
            entity.Property(e => e.RetentionDays).HasDefaultValue(14);
            entity.Property(e => e.Timezone).HasDefaultValueSql("'Asia/Ho_Chi_Minh'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.UpdatedByAccount).WithMany(p => p.BackupScheduleSettings)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_backup_schedule_settings_updated_by");
        });

        modelBuilder.Entity<CourseMilestone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("course_milestones_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MaxScore).HasDefaultValueSql("10.00");
            entity.Property(e => e.Position).HasDefaultValue(0L);
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.Type).HasDefaultValueSql("'CHECKPOINT'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Instructor).WithMany(p => p.CourseMilestones)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_course_milestones_instructor");
        });

        modelBuilder.Entity<GroupInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_invitations_pkey");

            entity.HasIndex(e => new { e.GroupId, e.InviteeStudentId }, "uq_group_invitations_pending")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'PENDING'::character varying");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupInvitations).HasConstraintName("fk_group_invitations_group");

            entity.HasOne(d => d.InviteeStudent).WithMany(p => p.GroupInvitationInviteeStudents).HasConstraintName("fk_group_invitations_invitee");

            entity.HasOne(d => d.InviterStudent).WithMany(p => p.GroupInvitationInviterStudents).HasConstraintName("fk_group_invitations_inviter");
        });

        modelBuilder.Entity<GroupJoinRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_join_requests_pkey");

            entity.HasIndex(e => new { e.GroupId, e.StudentId }, "uq_group_join_requests_pending")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'PENDING'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupJoinRequests).HasConstraintName("fk_group_join_requests_group");

            entity.HasOne(d => d.RespondedByStudent).WithMany(p => p.GroupJoinRequestRespondedByStudents)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_group_join_requests_responded_by");

            entity.HasOne(d => d.Student).WithMany(p => p.GroupJoinRequestStudents).HasConstraintName("fk_group_join_requests_student");
        });

        modelBuilder.Entity<GroupRecruitmentNeed>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_recruitment_needs_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupRecruitmentNeeds).HasConstraintName("fk_group_recruitment_needs_group");
        });

        modelBuilder.Entity<GroupTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_tasks_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Priority).HasDefaultValueSql("'MEDIUM'::character varying");
            entity.Property(e => e.Status).HasDefaultValueSql("'BACKLOG'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Version).HasDefaultValue(0L).IsConcurrencyToken();

            entity.HasOne(d => d.Board).WithMany(p => p.GroupTasks).HasConstraintName("fk_group_tasks_board");

            entity.HasOne(d => d.CreatedByStudent).WithMany(p => p.GroupTasks)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_group_tasks_created_by");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupTasks).HasConstraintName("fk_group_tasks_group");
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_batches_pkey");

            entity.Property(e => e.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ImportBatches)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_import_batches_creator");
        });

        modelBuilder.Entity<ImportRowError>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_row_errors_pkey");

            entity.HasOne(d => d.Batch).WithMany(p => p.ImportRowErrors).HasConstraintName("fk_import_row_errors_batch");
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("instructors_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithOne(p => p.Instructor).HasConstraintName("fk_instructors_account");
        });

        modelBuilder.Entity<Mentor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mentors_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithOne(p => p.Mentor).HasConstraintName("fk_mentors_account");
        });

        modelBuilder.Entity<MentorAvailabilitySlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mentor_availability_slots_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'AVAILABLE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Mentor).WithMany(p => p.MentorAvailabilitySlots).HasConstraintName("fk_mentor_availability_slots_mentor");
        });

        modelBuilder.Entity<MentorMeeting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mentor_meetings_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'SCHEDULED'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.BookedByStudent).WithMany(p => p.MentorMeetingBookedByStudents)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_mentor_meetings_booked_by");

            entity.HasOne(d => d.EvidenceSubmittedByStudent).WithMany(p => p.MentorMeetingEvidenceSubmittedByStudents)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_mentor_meetings_evidence_submitted_by");

            entity.HasOne(d => d.Group).WithMany(p => p.MentorMeetings).HasConstraintName("fk_mentor_meetings_group");

            entity.HasOne(d => d.LeaderConfirmedByStudent).WithMany(p => p.MentorMeetingLeaderConfirmedByStudents)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_mentor_meetings_leader_confirmed_by_student");

            entity.HasOne(d => d.Mentor).WithMany(p => p.MentorMeetings).HasConstraintName("fk_mentor_meetings_mentor");

            entity.HasOne(d => d.Slot).WithOne(p => p.MentorMeeting)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_mentor_meetings_slot");
        });

        modelBuilder.Entity<MilestoneContributionAgreement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_contribution_agreements_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RespondedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Revision).WithMany(p => p.MilestoneContributionAgreements).HasConstraintName("fk_contribution_agreements_revision");

            entity.HasOne(d => d.Student).WithMany(p => p.MilestoneContributionAgreements).HasConstraintName("fk_contribution_agreements_student");
        });

        modelBuilder.Entity<MilestoneContributionRevision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_contribution_revisions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Revision).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Group).WithMany(p => p.MilestoneContributionRevisions).HasConstraintName("fk_contribution_revisions_group");

            entity.HasOne(d => d.Milestone).WithMany(p => p.MilestoneContributionRevisions).HasConstraintName("fk_contribution_revisions_milestone");

            entity.HasOne(d => d.SubmittedByStudent).WithMany(p => p.MilestoneContributionRevisions).HasConstraintName("fk_contribution_revisions_submitter");
        });

        modelBuilder.Entity<MilestoneGrade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_grades_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.GradedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Instructor).WithMany(p => p.MilestoneGrades)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_milestone_grades_instructor");

            entity.HasOne(d => d.Submission).WithOne(p => p.MilestoneGrade).HasConstraintName("fk_milestone_grades_submission");
        });

        modelBuilder.Entity<MilestoneGroupGrade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_group_grades_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.GradedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Group).WithMany(p => p.MilestoneGroupGrades).HasConstraintName("fk_milestone_group_grades_group");

            entity.HasOne(d => d.Instructor).WithMany(p => p.MilestoneGroupGrades)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_milestone_group_grades_instructor");

            entity.HasOne(d => d.Milestone).WithMany(p => p.MilestoneGroupGrades).HasConstraintName("fk_milestone_group_grades_milestone");
        });

        modelBuilder.Entity<MilestoneMemberScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_member_scores_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.GroupGrade).WithMany(p => p.MilestoneMemberScores)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_milestone_member_scores_group_grade");

            entity.HasOne(d => d.Group).WithMany(p => p.MilestoneMemberScores).HasConstraintName("fk_milestone_member_scores_group");

            entity.HasOne(d => d.Milestone).WithMany(p => p.MilestoneMemberScores).HasConstraintName("fk_milestone_member_scores_milestone");

            entity.HasOne(d => d.Student).WithMany(p => p.MilestoneMemberScores).HasConstraintName("fk_milestone_member_scores_student");
        });

        modelBuilder.Entity<MilestoneOutcome>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_outcomes_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Milestone).WithMany(p => p.MilestoneOutcomes).HasConstraintName("fk_milestone_outcomes_milestone");
        });

        modelBuilder.Entity<MilestoneSubmission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("milestone_submissions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Late).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValueSql("'SUBMITTED'::character varying");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Version).HasDefaultValue(0L);

            entity.HasOne(d => d.Group).WithMany(p => p.MilestoneSubmissions).HasConstraintName("fk_milestone_submissions_group");

            entity.HasOne(d => d.Milestone).WithMany(p => p.MilestoneSubmissions).HasConstraintName("fk_milestone_submissions_milestone");

            entity.HasOne(d => d.SubmittedByStudent).WithMany(p => p.MilestoneSubmissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_milestone_submissions_submitter");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.HasIndex(e => new { e.RecipientId, e.CreatedAt, e.Id }, "idx_notifications_unread_created_id")
                .IsDescending(false, true, true)
                .HasFilter("(read_at IS NULL)");

            entity.HasIndex(e => new { e.RecipientId, e.EventKey }, "uq_notifications_recipient_event_key")
                .IsUnique()
                .HasFilter("(event_key IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Recipient).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notifications_recipient");
        });

        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problems_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SourceType).HasDefaultValueSql("'OFFICIAL'::character varying");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Domain).WithMany(p => p.Problems)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_problems_domain");

            entity.HasOne(d => d.ProposedByGroup).WithMany(p => p.Problems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_problems_proposed_by_group");

            entity.HasOne(d => d.ProposedByStudent).WithMany(p => p.Problems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_problems_proposed_by_student");

            entity.HasOne(d => d.ReviewedByAccount).WithMany(p => p.Problems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_problems_reviewed_by_account");
        });

        modelBuilder.Entity<ProblemDomain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_domains_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ProblemEvaluationCriterion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_evaluation_criteria_pkey");

            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens).HasConstraintName("fk_refresh_tokens_account");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Account).WithOne(p => p.Student).HasConstraintName("fk_students_account");
        });

        modelBuilder.Entity<StudentGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_groups_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValueSql("'ACTIVE'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.StudentGroups)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_student_groups_import_batch");

            entity.HasOne(d => d.Instructor).WithMany(p => p.StudentGroups)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_student_groups_instructor");

            entity.HasOne(d => d.LeaderStudent).WithMany(p => p.StudentGroups)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_student_groups_leader");

            entity.HasOne(d => d.Mentor).WithMany(p => p.StudentGroups)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_student_groups_mentor");

            entity.HasOne(d => d.SelectedProblem).WithMany(p => p.StudentGroups)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_student_groups_selected_problem");

            entity.HasOne(d => d.TermNavigation).WithMany(p => p.StudentGroups)
                .HasPrincipalKey(p => p.Code)
                .HasForeignKey(d => d.Term)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_student_groups_academic_term");
        });

        modelBuilder.Entity<StudentGroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_group_members_pkey");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MemberRole).HasDefaultValueSql("'MEMBER'::character varying");

            entity.HasOne(d => d.Group).WithMany(p => p.StudentGroupMembers).HasConstraintName("fk_student_group_members_group");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentGroupMembers).HasConstraintName("fk_student_group_members_student");
        });

        modelBuilder.Entity<TaskActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_activities_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Details).HasDefaultValueSql("'{}'::jsonb");

            entity.HasOne(d => d.ActorAccount).WithMany(p => p.TaskActivities)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_activities_actor");

            entity.HasOne(d => d.Group).WithMany(p => p.TaskActivities).HasConstraintName("fk_task_activities_group");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskActivities).HasConstraintName("fk_task_activities_task");
        });

        modelBuilder.Entity<TaskAssignee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_assignees_pkey");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.AssignedByStudent).WithMany(p => p.TaskAssigneeAssignedByStudents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_task_assignees_assigned_by");

            entity.HasOne(d => d.Student).WithMany(p => p.TaskAssigneeStudents).HasConstraintName("fk_task_assignees_student");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAssignees).HasConstraintName("fk_task_assignees_task");
        });

        modelBuilder.Entity<TaskBoard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_boards_pkey");

            entity.HasIndex(e => e.GroupId, "uq_task_boards_one_default_per_group")
                .IsUnique()
                .HasFilter("(default_board = true)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DefaultBoard).HasDefaultValue(false);
            entity.Property(e => e.Position).HasDefaultValue(0L);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.CreatedByStudent).WithMany(p => p.TaskBoards)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_boards_created_by");

            entity.HasOne(d => d.Group).WithMany(p => p.TaskBoards).HasConstraintName("fk_task_boards_group");
        });

        modelBuilder.Entity<TaskChecklistItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_checklist_items_pkey");

            entity.Property(e => e.Completed).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.CompletedByAccount).WithMany(p => p.TaskChecklistItems)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_checklist_items_completed_by");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskChecklistItems).HasConstraintName("fk_task_checklist_items_task");
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_comments_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.AuthorAccount).WithMany(p => p.TaskComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_task_comments_author");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskComments).HasConstraintName("fk_task_comments_task");
        });

        modelBuilder.Entity<TermFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("term_feedbacks_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'PENDING'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Version).HasDefaultValue(0L).IsConcurrencyToken();

            entity.HasOne(d => d.AcademicTerm).WithMany(p => p.TermFeedbacks).HasConstraintName("fk_term_feedbacks_academic_term");

            entity.HasOne(d => d.Group).WithMany(p => p.TermFeedbacks).HasConstraintName("fk_term_feedbacks_group");

            entity.HasOne(d => d.Instructor).WithMany(p => p.TermFeedbacks)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_term_feedbacks_instructor");

            entity.HasOne(d => d.Mentor).WithMany(p => p.TermFeedbacks)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_term_feedbacks_mentor");

            entity.HasOne(d => d.Student).WithMany(p => p.TermFeedbacks).HasConstraintName("fk_term_feedbacks_student");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
