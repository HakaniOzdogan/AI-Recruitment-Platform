namespace IkOtomasyon.Api.Entities;

public class PipelineStage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsTerminal { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
