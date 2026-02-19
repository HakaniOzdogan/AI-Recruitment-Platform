using System.Text.Json;
using NJsonSchema;

namespace IkOtomasyon.Api.Services;

public static class StructuredJsonValidator
{
    private static readonly HashSet<string> AllowedPlanCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "technical", "behavioral", "case", "culture"
    };

    private static readonly HashSet<string> AllowedQuestionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bank", "generated"
    };

    public static StructuredValidationResult Validate(string rawJson, string jsonSchema)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawJson);
        }
        catch (Exception ex)
        {
            return StructuredValidationResult.Fail(new StructuredValidationIssue("$", $"Invalid JSON: {ex.Message}"));
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return StructuredValidationResult.Fail(new StructuredValidationIssue("$", "Root must be a JSON object."));
            }

            var issues = new List<StructuredValidationIssue>();
            issues.AddRange(ValidateWithJsonSchema(rawJson, jsonSchema));
            issues.AddRange(ValidateDomainRules(document.RootElement, jsonSchema));
            return issues.Count == 0
                ? StructuredValidationResult.Valid
                : StructuredValidationResult.Fail(issues);
        }
    }

    private static IEnumerable<StructuredValidationIssue> ValidateWithJsonSchema(string rawJson, string jsonSchemaText)
    {
        JsonSchema schema;
        try
        {
            schema = JsonSchema.FromJsonAsync(jsonSchemaText).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return [new StructuredValidationIssue("$schema", $"Schema parse failed: {ex.Message}")];
        }

        var errors = schema.Validate(rawJson);
        return errors.Select(err => new StructuredValidationIssue(err.Path ?? "$", err.ToString()));
    }

    private static IEnumerable<StructuredValidationIssue> ValidateDomainRules(JsonElement root, string jsonSchema)
    {
        return IsPlanSchema(jsonSchema)
            ? ValidatePlanDomain(root)
            : IsAnalyzeSchema(jsonSchema)
                ? ValidateAnalyzeDomain(root)
                : Array.Empty<StructuredValidationIssue>();
    }

    private static IEnumerable<StructuredValidationIssue> ValidateAnalyzeDomain(JsonElement root)
    {
        var issues = new List<StructuredValidationIssue>();
        if (!TryGet(root, "signals", JsonValueKind.Array, out var signals))
        {
            issues.Add(new StructuredValidationIssue("$.signals", "signals is required."));
            return issues;
        }

        var index = 0;
        foreach (var signal in signals.EnumerateArray())
        {
            var prefix = $"$.signals[{index}]";
            ValidateRequiredString(signal, "term", prefix, issues);
            ValidateRequiredString(signal, "topic_key", prefix, issues);
            ValidateNumberRange(signal, "confidence", 0, 1, prefix, issues);
            index++;
        }

        if (TryGet(root, "evidence_snippets", JsonValueKind.Array, out var evidenceSnippets))
        {
            index = 0;
            foreach (var evidence in evidenceSnippets.EnumerateArray())
            {
                var prefix = $"$.evidence_snippets[{index}]";
                ValidateRequiredString(evidence, "quote", prefix, issues, maxLength: 200);
                ValidateRequiredString(evidence, "related_to", prefix, issues);
                index++;
            }
        }

        return issues;
    }

    private static IEnumerable<StructuredValidationIssue> ValidatePlanDomain(JsonElement root)
    {
        var issues = new List<StructuredValidationIssue>();
        if (!root.TryGetProperty("plan_horizon", out var planHorizon)
            || planHorizon.ValueKind != JsonValueKind.Number
            || !planHorizon.TryGetInt32(out var planHorizonValue)
            || (planHorizonValue is not 2 and not 3))
        {
            issues.Add(new StructuredValidationIssue("$.plan_horizon", "plan_horizon must be 2 or 3."));
        }

        if (!TryGet(root, "planned_questions", JsonValueKind.Array, out var plannedQuestions))
        {
            issues.Add(new StructuredValidationIssue("$.planned_questions", "planned_questions is required and must be array."));
            return issues;
        }

        var topicKeys = new List<string>();
        var index = 0;
        foreach (var question in plannedQuestions.EnumerateArray())
        {
            var prefix = $"$.planned_questions[{index}]";
            var type = ValidateRequiredString(question, "type", prefix, issues);
            var category = ValidateRequiredString(question, "category", prefix, issues);
            var topic = ValidateRequiredString(question, "topic_key", prefix, issues);
            ValidateRequiredString(question, "question_text", prefix, issues, maxLength: 350);
            ValidateRequiredString(question, "why", prefix, issues);
            ValidateNumberRange(question, "difficulty", 1, 5, prefix, issues, integerOnly: true);

            if (type is not null && !AllowedQuestionTypes.Contains(type))
            {
                issues.Add(new StructuredValidationIssue($"{prefix}.type", "type must be one of: bank, generated."));
            }

            if (category is not null && !AllowedPlanCategories.Contains(category))
            {
                issues.Add(new StructuredValidationIssue($"{prefix}.category", "category must be one of: technical, behavioral, case, culture."));
            }

            if (!question.TryGetProperty("guardrails_ok", out var guardrailsOk)
                || (guardrailsOk.ValueKind != JsonValueKind.True && guardrailsOk.ValueKind != JsonValueKind.False))
            {
                issues.Add(new StructuredValidationIssue($"{prefix}.guardrails_ok", "guardrails_ok must be boolean."));
            }

            topicKeys.Add(topic ?? string.Empty);
            index++;
        }

        for (var i = 1; i < topicKeys.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(topicKeys[i])
                && topicKeys[i].Equals(topicKeys[i - 1], StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StructuredValidationIssue($"$.planned_questions[{i}].topic_key", "Consecutive duplicate topic_key is not allowed."));
            }
        }

        return issues;
    }

    private static bool TryGet(JsonElement root, string propertyName, JsonValueKind expectedKind, out JsonElement value)
    {
        if (!root.TryGetProperty(propertyName, out value))
        {
            return false;
        }

        return value.ValueKind == expectedKind;
    }

    private static string? ValidateRequiredString(
        JsonElement element,
        string propertyName,
        string pathPrefix,
        List<StructuredValidationIssue> issues,
        int? maxLength = null)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", "Field is required and must be string."));
            return null;
        }

        var value = prop.GetString()?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", "Field cannot be empty."));
            return null;
        }

        if (maxLength is not null && value.Length > maxLength.Value)
        {
            issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", $"Field cannot exceed {maxLength.Value} chars."));
        }

        return value;
    }

    private static void ValidateNumberRange(
        JsonElement element,
        string propertyName,
        double min,
        double max,
        string pathPrefix,
        List<StructuredValidationIssue> issues,
        bool integerOnly = false)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Number)
        {
            issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", "Field is required and must be number."));
            return;
        }

        if (integerOnly)
        {
            if (!prop.TryGetInt32(out var intValue) || intValue < min || intValue > max)
            {
                issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", $"Field must be integer in range {min}..{max}."));
            }

            return;
        }

        if (!prop.TryGetDouble(out var value) || value < min || value > max)
        {
            issues.Add(new StructuredValidationIssue($"{pathPrefix}.{propertyName}", $"Field must be in range {min}..{max}."));
        }
    }

    private static bool IsAnalyzeSchema(string schema)
        => schema.Contains("\"signals\"", StringComparison.OrdinalIgnoreCase)
           && schema.Contains("\"next_focus\"", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlanSchema(string schema)
        => schema.Contains("\"planned_questions\"", StringComparison.OrdinalIgnoreCase);
}

public sealed record StructuredValidationIssue(string Path, string Message);

public sealed record StructuredValidationResult(bool IsValid, IReadOnlyCollection<StructuredValidationIssue> Errors)
{
    public static readonly StructuredValidationResult Valid = new(true, Array.Empty<StructuredValidationIssue>());

    public static StructuredValidationResult Fail(params StructuredValidationIssue[] errors)
        => new(false, errors);

    public static StructuredValidationResult Fail(IEnumerable<StructuredValidationIssue> errors)
        => new(false, errors.ToList());
}
