using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("student_groups")]
[Index("ImportBatchId", Name = "idx_student_groups_import_batch")]
[Index("InstructorId", Name = "idx_student_groups_instructor_id")]
[Index("MentorId", Name = "idx_student_groups_mentor_id")]
[Index("SelectedProblemId", Name = "idx_student_groups_selected_problem_id")]
[Index("Term", "CourseCode", Name = "idx_student_groups_term_course")]
public partial class StudentGroup
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("term")]
    [StringLength(30)]
    public string Term { get; set; } = null!;

    [Column("course_code")]
    [StringLength(30)]
    public string CourseCode { get; set; } = null!;

    [Column("group_no")]
    [StringLength(30)]
    public string GroupNo { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("project_name")]
    [StringLength(255)]
    public string? ProjectName { get; set; }

    [Column("idea_description")]
    public string? IdeaDescription { get; set; }

    [Column("research_domain")]
    public string? ResearchDomain { get; set; }

    [Column("leader_student_id")]
    public long? LeaderStudentId { get; set; }

    [Column("required_gpa")]
    [Precision(3, 2)]
    public decimal? RequiredGpa { get; set; }

    [Column("target_grade")]
    [Precision(3, 1)]
    public decimal? TargetGrade { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("import_batch_id")]
    public long? ImportBatchId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("mentor_id")]
    public long? MentorId { get; set; }

    [Column("selected_problem_id")]
    public long? SelectedProblemId { get; set; }

    [Column("instructor_id")]
    public long? InstructorId { get; set; }

    [Column("is_locked")]
    public bool IsLocked { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<GroupInvitation> GroupInvitations { get; set; } = new List<GroupInvitation>();

    [InverseProperty("Group")]
    public virtual ICollection<GroupJoinRequest> GroupJoinRequests { get; set; } = new List<GroupJoinRequest>();

    [InverseProperty("Group")]
    public virtual ICollection<GroupRecruitmentNeed> GroupRecruitmentNeeds { get; set; } = new List<GroupRecruitmentNeed>();

    [InverseProperty("Group")]
    public virtual ICollection<GroupTask> GroupTasks { get; set; } = new List<GroupTask>();

    [ForeignKey("ImportBatchId")]
    [InverseProperty("StudentGroups")]
    public virtual ImportBatch? ImportBatch { get; set; }

    [ForeignKey("InstructorId")]
    [InverseProperty("StudentGroups")]
    public virtual Instructor? Instructor { get; set; }

    [ForeignKey("LeaderStudentId")]
    [InverseProperty("StudentGroups")]
    public virtual Student? LeaderStudent { get; set; }

    [ForeignKey("MentorId")]
    [InverseProperty("StudentGroups")]
    public virtual Mentor? Mentor { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<MentorMeeting> MentorMeetings { get; set; } = new List<MentorMeeting>();

    [InverseProperty("Group")]
    public virtual ICollection<MilestoneContributionRevision> MilestoneContributionRevisions { get; set; } = new List<MilestoneContributionRevision>();

    [InverseProperty("Group")]
    public virtual ICollection<MilestoneGroupGrade> MilestoneGroupGrades { get; set; } = new List<MilestoneGroupGrade>();

    [InverseProperty("Group")]
    public virtual ICollection<MilestoneMemberScore> MilestoneMemberScores { get; set; } = new List<MilestoneMemberScore>();

    [InverseProperty("Group")]
    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    [InverseProperty("ProposedByGroup")]
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();

    [ForeignKey("SelectedProblemId")]
    [InverseProperty("StudentGroups")]
    public virtual Problem? SelectedProblem { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<StudentGroupMember> StudentGroupMembers { get; set; } = new List<StudentGroupMember>();

    [InverseProperty("Group")]
    public virtual ICollection<TaskActivity> TaskActivities { get; set; } = new List<TaskActivity>();

    [InverseProperty("Group")]
    public virtual ICollection<TaskBoard> TaskBoards { get; set; } = new List<TaskBoard>();

    [InverseProperty("Group")]
    public virtual ICollection<TermFeedback> TermFeedbacks { get; set; } = new List<TermFeedback>();

    [ForeignKey("Term")]
    [InverseProperty("StudentGroups")]
    public virtual AcademicTerm TermNavigation { get; set; } = null!;
}
