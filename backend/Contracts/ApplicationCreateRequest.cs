using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class ApplicationCreateRequest
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    public Guid CandidateId { get; set; }
}
