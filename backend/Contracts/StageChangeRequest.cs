using System.ComponentModel.DataAnnotations;

namespace IkOtomasyon.Api.Contracts;

public class StageChangeRequest
{
    [Required]
    public Guid StageId { get; set; }
}
