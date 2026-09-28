using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("problems")]
[Index("DifficultyLevel", Name = "idx_problems_difficulty_level")]
[Index("DomainId", Name = "idx_problems_domain_id")]
[Index("ExpectedOutput", Name = "idx_problems_expected_output")]
[Index("ProposedByGroupId", Name = "idx_problems_proposed_by_group_id")]
[Index("ProposedByStudentId", Name = "idx_problems_proposed_by_student_id")]
[Index("ReviewedByAccountId", Name = "idx_problems_reviewed_by_account_id")]
[Index("SourceType", Name = "idx_problems_source_type")]
[Index("Status", Name = "idx_problems_status")]
[Index("Code", Name = "problems_code_key", IsUnique = true)]
public partial class Problem
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("domain_id")]
    public long? DomainId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("statement")]
    public string Statement { get; set; } = null!;

    [Column("code")]
    [StringLength(100)]
    public string? Code { get; set; }

    [Column("strategic_theme")]
    [StringLength(255)]
    public string? StrategicTheme { get; set; }

    [Column("research_area")]
    [StringLength(255)]
    public string? ResearchArea { get; set; }

    [Column("difficulty_level")]
    [StringLength(30)]
    public string DifficultyLevel { get; set; } = null!;

    [Column("expected_output")]
    public string? ExpectedOutput { get; set; }

    [Column("source_type")]
    [StringLength(30)]
    public string SourceType { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("proposed_by_group_id")]
    public long? ProposedByGroupId { get; set; }

    [Column("proposed_by_student_id")]
    public long? ProposedByStudentId { get; set; }

    [Column("review_comment")]
    public string? ReviewComment { get; set; }

    [Column("reviewed_by_account_id")]
    public long? ReviewedByAccountId { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("owner_lab")]
    [StringLength(255)]
    public string? OwnerLab { get; set; }

    [Column("suggested_courses")]
    public string? SuggestedCourses { get; set; }

    [Column("drive_folder_link")]
    [StringLength(500)]
    public string? DriveFolderLink { get; set; }

    [ForeignKey("DomainId")]
    [InverseProperty("Problems")]
    public virtual ProblemDomain? Domain { get; set; }

    [ForeignKey("ProposedByGroupId")]
    [InverseProperty("Problems")]
    public virtual StudentGroup? ProposedByGroup { get; set; }

    [ForeignKey("ProposedByStudentId")]
    [InverseProperty("Problems")]
    public virtual Student? ProposedByStudent { get; set; }

    [ForeignKey("ReviewedByAccountId")]
    [InverseProperty("Problems")]
    public virtual Account? ReviewedByAccount { get; set; }

    [InverseProperty("SelectedProblem")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
}
