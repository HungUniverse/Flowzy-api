using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("students")]
[Index("Cohort", "ClassName", Name = "idx_students_cohort_class")]
[Index("FullName", Name = "idx_students_full_name")]
[Index("AccountId", Name = "students_account_id_key", IsUnique = true)]
[Index("StudentCode", Name = "students_student_code_key", IsUnique = true)]
public partial class Student
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("student_code")]
    [StringLength(50)]
    public string StudentCode { get; set; } = null!;

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("gender")]
    [StringLength(20)]
    public string? Gender { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("major")]
    [StringLength(150)]
    public string? Major { get; set; }

    [Column("cohort")]
    [StringLength(50)]
    public string? Cohort { get; set; }

    [Column("class_name")]
    [StringLength(100)]
    public string? ClassName { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("Student")]
    public virtual Account Account { get; set; } = null!;

    [InverseProperty("InviteeStudent")]
    public virtual ICollection<GroupInvitation> GroupInvitationInviteeStudents { get; set; } = new List<GroupInvitation>();

    [InverseProperty("InviterStudent")]
    public virtual ICollection<GroupInvitation> GroupInvitationInviterStudents { get; set; } = new List<GroupInvitation>();

    [InverseProperty("RespondedByStudent")]
    public virtual ICollection<GroupJoinRequest> GroupJoinRequestRespondedByStudents { get; set; } = new List<GroupJoinRequest>();

    [InverseProperty("Student")]
    public virtual ICollection<GroupJoinRequest> GroupJoinRequestStudents { get; set; } = new List<GroupJoinRequest>();

    [InverseProperty("CreatedByStudent")]
    public virtual ICollection<GroupTask> GroupTasks { get; set; } = new List<GroupTask>();

    [InverseProperty("BookedByStudent")]
    public virtual ICollection<MentorMeeting> MentorMeetingBookedByStudents { get; set; } = new List<MentorMeeting>();

    [InverseProperty("EvidenceSubmittedByStudent")]
    public virtual ICollection<MentorMeeting> MentorMeetingEvidenceSubmittedByStudents { get; set; } = new List<MentorMeeting>();

    [InverseProperty("LeaderConfirmedByStudent")]
    public virtual ICollection<MentorMeeting> MentorMeetingLeaderConfirmedByStudents { get; set; } = new List<MentorMeeting>();

    [InverseProperty("Student")]
    public virtual ICollection<MilestoneContributionAgreement> MilestoneContributionAgreements { get; set; } = new List<MilestoneContributionAgreement>();

    [InverseProperty("SubmittedByStudent")]
    public virtual ICollection<MilestoneContributionRevision> MilestoneContributionRevisions { get; set; } = new List<MilestoneContributionRevision>();

    [InverseProperty("Student")]
    public virtual ICollection<MilestoneMemberScore> MilestoneMemberScores { get; set; } = new List<MilestoneMemberScore>();

    [InverseProperty("SubmittedByStudent")]
    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();

    [InverseProperty("ProposedByStudent")]
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();

    [InverseProperty("Student")]
    public virtual ICollection<StudentGroupMember> StudentGroupMembers { get; set; } = new List<StudentGroupMember>();

    [InverseProperty("LeaderStudent")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();

    [InverseProperty("AssignedByStudent")]
    public virtual ICollection<TaskAssignee> TaskAssigneeAssignedByStudents { get; set; } = new List<TaskAssignee>();

    [InverseProperty("Student")]
    public virtual ICollection<TaskAssignee> TaskAssigneeStudents { get; set; } = new List<TaskAssignee>();

    [InverseProperty("CreatedByStudent")]
    public virtual ICollection<TaskBoard> TaskBoards { get; set; } = new List<TaskBoard>();

    [InverseProperty("Student")]
    public virtual ICollection<TermFeedback> TermFeedbacks { get; set; } = new List<TermFeedback>();
}
