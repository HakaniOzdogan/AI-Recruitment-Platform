using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class InterviewMessageRequest
{
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}
