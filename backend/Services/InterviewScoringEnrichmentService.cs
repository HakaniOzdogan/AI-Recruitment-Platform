using System.Text.Json;
using System.Text.Json.Serialization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Entities;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Services;

public class InterviewScoringEnrichmentService
{
    private static readonly HashSet<string> AllowedCriterionKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        RubricKeys.Technical,
        RubricKeys.ProblemSolving,
        RubricKeys.Communication,
        RubricKeys.CultureFit,
        RubricKeys.DomainKnowledge
    };

    private static readonly string[] SensitiveKeywords =
    [
        "health", "religion", "politics", "race", "ethnicity", "sexual", "union",
        "sağlık", "din", "siyaset", "ırk", "etnik", "cinsel", "sendika"
    ];

    private readonly ILLMClient _llmClient;
    private readonly LlmOptions _llmOptions;

    public InterviewScoringEnrichmentService(ILLMClient llmClient, IOptions<LlmOptions> llmOptions)
    {
        _llmClient = llmClient;
        _llmOptions = llmOptions.Value;
    }

    public async Task<ScoreEnrichmentResult> EnrichAsync(ScoreEnrichmentRequest request, CancellationToken ct = default)
    {
        var userPrompt = JsonSerializer.Serialize(new
        {
            criterion = request.CriterionKey,
            job = request.JobSummary,
            deterministic = new
            {
                request.DeterministicScore,
                request.DeterministicRationale,
                evidence = request.DeterministicEvidence
            },
            transcript_snippets = request.TranscriptSnippets.Take(8)
        });

        try
        {
            var raw = await _llmClient.GenerateStructuredAsync(SystemPrompt, userPrompt, Schema, ct);
            var validation = StructuredJsonValidator.Validate(raw, Schema);
            if (!validation.IsValid)
            {
                return ScoreEnrichmentResult.Failed(validation.Errors.Select(x => $"{x.Path}: {x.Message}"));
            }

            var output = JsonSerializer.Deserialize<EnrichmentOutput>(raw, JsonOptions);
            if (output is null)
            {
                return ScoreEnrichmentResult.Failed(["LLM output deserialize failed."]);
            }

            var guardrailError = GuardrailCheck(output);
            if (guardrailError is not null)
            {
                return ScoreEnrichmentResult.GuardrailViolation([guardrailError]);
            }

            var additionalEvidence = (output.AdditionalEvidenceQuotes ?? new List<EnrichmentEvidence>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Quote))
                .Select(x => new EvidenceQuoteResponse(SanitizeQuote(x.Quote!), SanitizeRelatedTo(x.RelatedTo)))
                .ToList();

            return ScoreEnrichmentResult.Enriched(
                Math.Clamp(output.SuggestedScore, 0, 5),
                output.Rationale.Trim(),
                additionalEvidence,
                Math.Clamp(output.Confidence, 0, 1),
                _llmOptions.Model);
        }
        catch (LlmSchemaException ex)
        {
            return ScoreEnrichmentResult.Failed([ex.Message]);
        }
        catch (LlmTimeoutException ex)
        {
            return ScoreEnrichmentResult.Failed([ex.Message]);
        }
        catch (LlmProviderException ex)
        {
            return ScoreEnrichmentResult.Failed([ex.Message]);
        }
    }

    private static string? GuardrailCheck(EnrichmentOutput output)
    {
        if (!AllowedCriterionKeys.Contains(output.CriterionKey ?? string.Empty))
        {
            return "criterion_key is not allowed.";
        }

        if (string.IsNullOrWhiteSpace(output.Rationale))
        {
            return "rationale is required.";
        }

        if (output.Rationale.Length > 800)
        {
            return "rationale exceeds max length.";
        }

        if (ContainsSensitive(output.Rationale))
        {
            return "Sensitive content detected in rationale.";
        }

        if (output.Confidence < 0 || output.Confidence > 1)
        {
            return "confidence must be 0..1.";
        }

        if (output.SuggestedScore < 0 || output.SuggestedScore > 5)
        {
            return "suggested_score must be 0..5.";
        }

        foreach (var item in output.AdditionalEvidenceQuotes ?? new List<EnrichmentEvidence>())
        {
            var quote = item.Quote?.Trim() ?? string.Empty;
            if (quote.Length > 200)
            {
                return "additional_evidence_quotes.quote exceeds max length.";
            }

            if (ContainsSensitive(quote))
            {
                return "Sensitive content detected in additional_evidence_quotes.";
            }
        }

        return null;
    }

    private static bool ContainsSensitive(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return SensitiveKeywords.Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeQuote(string quote)
    {
        var clean = quote.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return clean.Length > 200 ? clean[..200] : clean;
    }

    private static string SanitizeRelatedTo(string? relatedTo)
    {
        var value = string.IsNullOrWhiteSpace(relatedTo) ? "general" : relatedTo.Trim();
        return value.Length > 120 ? value[..120] : value;
    }

    private const string SystemPrompt = """
You are a rubric scoring assistant.
Use only provided transcript snippets and deterministic evidence.
Do not fabricate facts.
Do not include sensitive attributes (health, religion, politics, race, ethnicity, sexual life, union).
Return JSON only.
""";

    private const string Schema = """
{
  "$schema":"https://json-schema.org/draft/2020-12/schema",
  "type":"object",
  "required":["criterion_key","suggested_score","rationale","additional_evidence_quotes","confidence"],
  "properties":{
    "criterion_key":{"type":"string","enum":["technical","problem_solving","communication","culture_fit","domain_knowledge"]},
    "suggested_score":{"type":"integer","minimum":0,"maximum":5},
    "rationale":{"type":"string","minLength":1,"maxLength":800},
    "additional_evidence_quotes":{
      "type":"array",
      "items":{
        "type":"object",
        "required":["quote","related_to"],
        "properties":{
          "quote":{"type":"string","maxLength":200},
          "related_to":{"type":"string","maxLength":120}
        }
      }
    },
    "confidence":{"type":"number","minimum":0,"maximum":1}
  }
}
""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class EnrichmentOutput
    {
        [JsonPropertyName("criterion_key")]
        public string? CriterionKey { get; set; }
        [JsonPropertyName("suggested_score")]
        public int SuggestedScore { get; set; }
        [JsonPropertyName("rationale")]
        public string Rationale { get; set; } = string.Empty;
        [JsonPropertyName("additional_evidence_quotes")]
        public List<EnrichmentEvidence> AdditionalEvidenceQuotes { get; set; } = new();
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    private sealed class EnrichmentEvidence
    {
        [JsonPropertyName("quote")]
        public string? Quote { get; set; }
        [JsonPropertyName("related_to")]
        public string? RelatedTo { get; set; }
    }
}

public sealed record ScoreEnrichmentRequest(
    string CriterionKey,
    string JobSummary,
    IReadOnlyCollection<string> TranscriptSnippets,
    IReadOnlyCollection<EvidenceQuoteResponse> DeterministicEvidence,
    double DeterministicScore,
    string? DeterministicRationale);

public sealed record ScoreEnrichmentResult(
    bool IsEnriched,
    bool GuardrailBlocked,
    int? SuggestedScore,
    string? Rationale,
    IReadOnlyCollection<EvidenceQuoteResponse> AdditionalEvidence,
    double? Confidence,
    string? ModelName,
    IReadOnlyCollection<string> Errors)
{
    public static ScoreEnrichmentResult Enriched(
        int suggestedScore,
        string rationale,
        IReadOnlyCollection<EvidenceQuoteResponse> additionalEvidence,
        double confidence,
        string modelName)
        => new(true, false, suggestedScore, rationale, additionalEvidence, confidence, modelName, Array.Empty<string>());

    public static ScoreEnrichmentResult Failed(IEnumerable<string> errors)
        => new(false, false, null, null, Array.Empty<EvidenceQuoteResponse>(), null, null, errors.ToList());

    public static ScoreEnrichmentResult GuardrailViolation(IEnumerable<string> errors)
        => new(false, true, null, null, Array.Empty<EvidenceQuoteResponse>(), null, null, errors.ToList());
}
