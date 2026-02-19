namespace IkOtomasyon.Api.Entities;

public class RubricCriterion
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public RubricTemplate? Template { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int Order { get; set; }
}
