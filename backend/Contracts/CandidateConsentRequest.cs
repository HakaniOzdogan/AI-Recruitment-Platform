using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class CandidateConsentRequest
{
    [Required]
    public bool ConsentGiven { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConsentTextVersion { get; set; } = string.Empty;

    [Range(1, 3650)]
    public int DataRetentionDays { get; set; } = 365;
}
