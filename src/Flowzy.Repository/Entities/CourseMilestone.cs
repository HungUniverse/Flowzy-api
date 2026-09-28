using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("course_milestones")]
[Index("InstructorId", Name = "idx_course_milestones_instructor_id")]
[Index("InstructorId", "Term", "CourseCode", "Status", "Position", Name = "idx_course_milestones_owner_timeline")]
[Index("Term", "CourseCode", Name = "idx_course_milestones_term_course")]
[Index("InstructorId", "Term", "CourseCode", "Title", Name = "uq_course_milestones_owner_term_course_title", IsUnique = true)]
public partial class CourseMilestone
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("instructor_id")]
    public long? InstructorId { get; set; }

    [Column("term")]
    [StringLength(30)]
    public string Term { get; set; } = null!;

    [Column("course_code")]
    [StringLength(30)]
    public string CourseCode { get; set; } = null!;

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("weight")]
    public int? Weight { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("type")]
    [StringLength(30)]
    public string Type { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("max_score")]
    [Precision(5, 2)]
    public decimal MaxScore { get; set; }

    [Column("position")]
    public long Position { get; set; }

    [ForeignKey("InstructorId")]
    [InverseProperty("CourseMilestones")]
    public virtual Instructor? Instructor { get; set; }

    [InverseProperty("Milestone")]
    public virtual ICollection<MilestoneContributionRevision> MilestoneContributionRevisions { get; set; } = new List<MilestoneContributionRevision>();

    [InverseProperty("Milestone")]
    public virtual ICollection<MilestoneGroupGrade> MilestoneGroupGrades { get; set; } = new List<MilestoneGroupGrade>();

    [InverseProperty("Milestone")]
    public virtual ICollection<MilestoneMemberScore> MilestoneMemberScores { get; set; } = new List<MilestoneMemberScore>();

    [InverseProperty("Milestone")]
    public virtual ICollection<MilestoneOutcome> MilestoneOutcomes { get; set; } = new List<MilestoneOutcome>();

    [InverseProperty("Milestone")]
    public virtual ICollection<MilestoneSubmission> MilestoneSubmissions { get; set; } = new List<MilestoneSubmission>();
}
