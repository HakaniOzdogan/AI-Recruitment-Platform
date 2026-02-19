using System.Text.Json.Serialization;

namespace IkOtomasyon.Api.Services;

public class AiEvaluationOutput
{
    [JsonPropertyName("overall_recommendation")]
    public string OverallRecommendation { get; set; } = "hold";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("competency_assessment")]
    public List<AiCompetencyAssessment> CompetencyAssessment { get; set; } = new();

    [JsonPropertyName("skill_assessment")]
    public AiSkillAssessment SkillAssessment { get; set; } = new();

    [JsonPropertyName("strengths")]
    public List<AiInsight> Strengths { get; set; } = new();

    [JsonPropertyName("risks")]
    public List<AiRiskInsight> Risks { get; set; } = new();

    [JsonPropertyName("verification_questions")]
    public List<AiVerificationQuestion> VerificationQuestions { get; set; } = new();

    [JsonPropertyName("evidence_quotes")]
    public List<AiEvidenceQuote> EvidenceQuotes { get; set; } = new();
}

public class AiCompetencyAssessment
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [JsonPropertyName("evidence_quotes")]
    public List<AiEvidenceSnippet> EvidenceQuotes { get; set; } = new();
}

public class AiSkillAssessment
{
    [JsonPropertyName("weighted_coverage")]
    public double WeightedCoverage { get; set; }

    [JsonPropertyName("matched")]
    public List<AiMatchedSkill> Matched { get; set; } = new();

    [JsonPropertyName("missing_required")]
    public List<string> MissingRequired { get; set; } = new();

    [JsonPropertyName("missing_nice")]
    public List<string> MissingNice { get; set; } = new();
}

public class AiMatchedSkill
{
    [JsonPropertyName("skill")]
    public string Skill { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; }
}

public class AiInsight
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    [JsonPropertyName("evidence_quotes")]
    public List<AiEvidenceSnippet> EvidenceQuotes { get; set; } = new();
}

public class AiRiskInsight : AiInsight
{
    [JsonPropertyName("severity")]
    public int Severity { get; set; }
}

public class AiVerificationQuestion
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;
}

public class AiEvidenceQuote
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = "cv_profile";

    [JsonPropertyName("related_to")]
    public string RelatedTo { get; set; } = string.Empty;
}

public class AiEvidenceSnippet
{
    [JsonPropertyName("quote")]
    public string Quote { get; set; } = string.Empty;

    [JsonPropertyName("related_to")]
    public string RelatedTo { get; set; } = string.Empty;
}
