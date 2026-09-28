using System.ComponentModel.DataAnnotations;

namespace Flowzy.Service.Contracts;

public sealed record ReviewProblemRequest(
    [Required(ErrorMessage = "Review status is required")] string Status,
    string? Comment);
