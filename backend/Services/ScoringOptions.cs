namespace IkOtomasyon.Api.Services;

public class ScoringOptions
{
    public const string SectionName = "Scoring";

    public string Mode { get; set; } = "deterministic";
    public bool LlmScoringEnabled { get; set; } = false;
}
