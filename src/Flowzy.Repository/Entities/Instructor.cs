using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("instructors")]
[Index("FullName", Name = "idx_instructors_full_name")]
[Index("AccountId", Name = "instructors_account_id_key", IsUnique = true)]
[Index("InstructorCode", Name = "instructors_instructor_code_key", IsUnique = true)]
public partial class Instructor
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("instructor_code")]
    [StringLength(50)]
    public string InstructorCode { get; set; } = null!;

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("department")]
    [StringLength(150)]
    public string? Department { get; set; }

    [Column("expertise")]
    public string? Expertise { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("Instructor")]
    public virtual Account Account { get; set; } = null!;

    [InverseProperty("Instructor")]
    public virtual ICollection<CourseMilestone> CourseMilestones { get; set; } = new List<CourseMilestone>();

    [InverseProperty("Instructor")]
    public virtual ICollection<MilestoneGrade> MilestoneGrades { get; set; } = new List<MilestoneGrade>();

    [InverseProperty("Instructor")]
    public virtual ICollection<MilestoneGroupGrade> MilestoneGroupGrades { get; set; } = new List<MilestoneGroupGrade>();

    [InverseProperty("Instructor")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();

    [InverseProperty("Instructor")]
    public virtual ICollection<TermFeedback> TermFeedbacks { get; set; } = new List<TermFeedback>();
}
