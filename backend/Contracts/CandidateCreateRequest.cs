using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class CandidateCreateRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }
}
