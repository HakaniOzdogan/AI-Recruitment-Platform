namespace IkOtomasyon.Api.Services;

public class AdaptiveInterviewOptions
{
    public const string SectionName = "AdaptiveInterview";

    public bool Enabled { get; set; } = false;
    public int LastMessageWindow { get; set; } = 8;
    public int PlanHorizonMin { get; set; } = 2;
    public int PlanHorizonMax { get; set; } = 3;
}
