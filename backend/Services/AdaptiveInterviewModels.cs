using System.Text.Json.Serialization;

namespace IkOtomasyon.Api.Services;

public class AdaptiveAnalyzeOutput
{
    [JsonPropertyName("signals")]
    public List<AdaptiveSignal> Signals { get; set; } = new();

    [JsonPropertyName("competency_estimates")]
    public Dictionary<string, double> CompetencyEstimates { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("depth")]
    public Dictionary<string, double> Depth { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("risk_flags")]
    public Dictionary<string, bool> RiskFlags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("evidence_snippets")]
    public List<AdaptiveEvidence> EvidenceSnippets { get; set; } = new();

    [JsonPropertyName("next_focus")]
    public List<AdaptiveNextFocus> NextFocus { get; set; } = new();
}

public class AdaptiveSignal
{
    [JsonPropertyName("term")]
    public string Term { get; set; } = string.Empty;

    [JsonPropertyName("topic_key")]
    public string TopicKey { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class AdaptiveEvidence
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("related_to")]
    public string RelatedTo { get; set; } = string.Empty;
}

public class AdaptiveNextFocus
{
    [JsonPropertyName("topic_key")]
    public string TopicKey { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}

public class AdaptivePlanOutput
{
    [JsonPropertyName("plan_horizon")]
    public int PlanHorizon { get; set; }

    [JsonPropertyName("planned_questions")]
    public List<AdaptivePlannedQuestion> PlannedQuestions { get; set; } = new();
}

public class AdaptivePlannedQuestion
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "generated";

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("topic_key")]
    public string TopicKey { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [JsonPropertyName("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;

    [JsonPropertyName("guardrails_ok")]
    public bool GuardrailsOk { get; set; }
}
