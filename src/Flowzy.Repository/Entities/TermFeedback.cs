using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("term_feedbacks")]
[Index("GroupId", Name = "idx_term_feedbacks_group_id")]
[Index("InstructorId", "AcademicTermId", "Status", Name = "idx_term_feedbacks_instructor_term_status")]
[Index("MentorId", "AcademicTermId", "Status", Name = "idx_term_feedbacks_mentor_term_status")]
[Index("StudentId", "Status", Name = "idx_term_feedbacks_student_status")]
[Index("TargetType", Name = "idx_term_feedbacks_target_type")]
[Index("AcademicTermId", "GroupId", "StudentId", "TargetType", Name = "uq_term_feedbacks_term_group_student_target", IsUnique = true)]
public partial class TermFeedback
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("academic_term_id")]
    public long AcademicTermId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("target_type")]
    [StringLength(30)]
    public string TargetType { get; set; } = null!;

    [Column("mentor_id")]
    public long? MentorId { get; set; }

    [Column("instructor_id")]
    public long? InstructorId { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("version")]
    public long Version { get; set; }

    [ForeignKey("AcademicTermId")]
    [InverseProperty("TermFeedbacks")]
    public virtual AcademicTerm AcademicTerm { get; set; } = null!;

    [ForeignKey("GroupId")]
    [InverseProperty("TermFeedbacks")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("InstructorId")]
    [InverseProperty("TermFeedbacks")]
    public virtual Instructor? Instructor { get; set; }

    [ForeignKey("MentorId")]
    [InverseProperty("TermFeedbacks")]
    public virtual Mentor? Mentor { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("TermFeedbacks")]
    public virtual Student Student { get; set; } = null!;
}
