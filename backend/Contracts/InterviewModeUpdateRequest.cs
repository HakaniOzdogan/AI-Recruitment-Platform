using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class InterviewModeUpdateRequest
{
    [Required]
    [RegularExpression("^(OFF|ASSIST|ADAPTIVE)$", ErrorMessage = "aiMode must be OFF, ASSIST, or ADAPTIVE.")]
    public string AiMode { get; set; } = "OFF";
}
