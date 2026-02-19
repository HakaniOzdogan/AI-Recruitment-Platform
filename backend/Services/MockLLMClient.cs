using System.Text.Json;

namespace IkOtomasyon.Api.Services;

public class MockLLMClient : ILLMClient
{
    public Task<string> GenerateStructuredAsync(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken ct = default)
    {
        if (systemPrompt.Contains("INTERVIEW_ANALYZE_JSON", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(BuildInterviewAnalyzeOutput(userPrompt));
        }

        if (systemPrompt.Contains("INTERVIEW_PLAN_JSON", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(BuildInterviewPlanOutput(userPrompt));
        }

        var input = JsonSerializer.Deserialize<MockInput>(userPrompt, JsonSerializerOptions) ?? new MockInput();
        var candidateSkills = input.Profile.Skills
            .Select(SkillNormalization.Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var weightedCoverage = ComputeWeightedCoverage(input.SkillWeights, candidateSkills, out var matched, out var missingRequired, out var missingNice);
        var evidence = BuildEvidence(input, matched, missingRequired);
        var competencies = BuildCompetencyAssessment(input.CompetencyWeights, weightedCoverage, evidence);
        var strengths = BuildStrengths(matched, evidence);
        var risks = BuildRisks(missingRequired, missingNice, input, evidence);

        var recommendation = missingRequired.Count == 0
            ? (weightedCoverage >= 0.60 ? "shortlist" : "hold")
            : (weightedCoverage >= 0.40 ? "hold" : "reject");

        var confidence = Math.Clamp(weightedCoverage * 0.8 + 0.15, 0.05, 0.95);

        var output = new
        {
            overall_recommendation = recommendation,
            confidence,
            competency_assessment = competencies,
            skill_assessment = new
            {
                weighted_coverage = weightedCoverage,
                matched,
                missing_required = missingRequired,
                missing_nice = missingNice
            },
            strengths,
            risks,
            verification_questions = new[]
            {
                new
                {
                    category = "technical",
                    question = "Can you walk through one project where you used the core required stack end-to-end?",
                    why = "Validate depth of hands-on technical ownership against claimed skills."
                },
                new
                {
                    category = "behavioral",
                    question = "Tell us about a time you had conflicting requirements and how you resolved them.",
                    why = "Validate problem solving and communication under ambiguity."
                }
            },
            evidence_quotes = evidence.Select(x => new { quote = x.Quote, source = "cv_profile", related_to = x.RelatedTo }).ToList()
        };

        return Task.FromResult(JsonSerializer.Serialize(output));
    }

    private static string BuildInterviewAnalyzeOutput(string userPrompt)
    {
        var input = JsonSerializer.Deserialize<MockInterviewAnalyzeInput>(userPrompt, JsonSerializerOptions) ?? new MockInterviewAnalyzeInput();
        var candidateText = string.Join(" ", input.Messages
            .Where(x => x.Role.Equals("candidate", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Content));

        var terms = new[] { "cqrs", "sql", "docker", "kubernetes", "redis", "microservice", "trade-off" }
            .Where(x => candidateText.Contains(x, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var evidence = input.Messages
            .Where(x => x.Role.Equals("candidate", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Content.Trim())
            .Where(x => x.Length > 0)
            .TakeLast(3)
            .Select(x => new { quote = x.Length > 200 ? x[..200] : x, related_to = "technical" })
            .ToList();

        var output = new
        {
            signals = terms.Select(x => new { term = x, topic_key = x, confidence = 0.75 }).ToList(),
            competency_estimates = new
            {
                technical = terms.Count > 0 ? 3.5 : 2.5,
                problem_solving = candidateText.Contains("trade-off", StringComparison.OrdinalIgnoreCase) ? 3.2 : 2.3,
                communication = candidateText.Length > 120 ? 3.0 : 2.2,
                culture_fit = candidateText.Contains("takım", StringComparison.OrdinalIgnoreCase) ? 3.1 : 2.4,
                domain_knowledge = terms.Count > 1 ? 3.0 : 2.2
            },
            depth = new
            {
                clarity = candidateText.Length > 100 ? 3.0 : 2.0,
                specificity = candidateText.Any(char.IsDigit) ? 3.2 : 2.1
            },
            risk_flags = new
            {
                vague_answer = candidateText.Length < 60,
                contradiction = false,
                overclaim = false
            },
            evidence_snippets = evidence,
            next_focus = terms.Take(3).Select((x, i) => new { topic_key = x, why = "Probe implementation details.", priority = i + 1 }).ToList()
        };

        return JsonSerializer.Serialize(output);
    }

    private static string BuildInterviewPlanOutput(string userPrompt)
    {
        var input = JsonSerializer.Deserialize<MockInterviewPlanInput>(userPrompt, JsonSerializerOptions) ?? new MockInterviewPlanInput();
        var focusTopics = input.Analyze.NextFocus
            .Select(x => x.TopicKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .DefaultIfEmpty("system_design")
            .Take(3)
            .ToList();

        var questions = new List<object>();
        var categories = new[] { "technical", "behavioral", "case" };
        for (var i = 0; i < focusTopics.Count; i++)
        {
            var topic = focusTopics[i]!;
            questions.Add(new
            {
                type = "generated",
                category = categories[i % categories.Length],
                topic_key = topic,
                difficulty = 3,
                question_text = $"{topic} konusunda gerçek bir örnek verip karar adımlarını anlatır mısın?",
                why = "Mock adaptive follow-up",
                guardrails_ok = true
            });
        }

        var output = new
        {
            plan_horizon = Math.Clamp(questions.Count, 2, 3),
            planned_questions = questions
        };

        return JsonSerializer.Serialize(output);
    }

    private static double ComputeWeightedCoverage(
        List<MockSkillWeight> weights,
        HashSet<string> candidateSkills,
        out List<object> matched,
        out List<string> missingRequired,
        out List<string> missingNice)
    {
        matched = new List<object>();
        missingRequired = new List<string>();
        missingNice = new List<string>();

        if (weights.Count == 0)
        {
            return 0;
        }

        var weightSum = weights.Sum(x => Math.Max(0, x.Weight));
        if (weightSum <= 0)
        {
            return 0;
        }

        var matchedWeight = 0.0;
        foreach (var item in weights)
        {
            var normalized = SkillNormalization.Normalize(item.Name);
            if (candidateSkills.Contains(normalized))
            {
                matched.Add(new { skill = normalized, weight = item.Weight, is_required = item.IsRequired });
                matchedWeight += Math.Max(0, item.Weight);
            }
            else if (item.IsRequired)
            {
                missingRequired.Add(normalized);
            }
            else
            {
                missingNice.Add(normalized);
            }
        }

        missingRequired = missingRequired.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        missingNice = missingNice.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        return Math.Clamp(matchedWeight / weightSum, 0, 1);
    }

    private static List<AiEvidenceSnippet> BuildEvidence(MockInput input, List<object> matched, List<string> missingRequired)
    {
        var evidence = new List<AiEvidenceSnippet>();
        if (!string.IsNullOrWhiteSpace(input.Profile.Summary))
        {
            var summary = input.Profile.Summary.Length > 220 ? input.Profile.Summary[..220] : input.Profile.Summary;
            evidence.Add(new AiEvidenceSnippet { Quote = summary, RelatedTo = "summary" });
        }

        if (matched.Count > 0)
        {
            evidence.Add(new AiEvidenceSnippet
            {
                Quote = $"Detected matched skills from profile: {string.Join(", ", matched.Select(x => x.GetType().GetProperty("skill")?.GetValue(x)?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)))}",
                RelatedTo = "skills"
            });
        }

        if (missingRequired.Count > 0)
        {
            evidence.Add(new AiEvidenceSnippet
            {
                Quote = $"No direct evidence found for required skills: {string.Join(", ", missingRequired)}",
                RelatedTo = "missing_required_skills"
            });
        }

        if (input.Profile.TotalExperienceMonths is not null)
        {
            evidence.Add(new AiEvidenceSnippet
            {
                Quote = $"Total detected experience: {input.Profile.TotalExperienceMonths} months.",
                RelatedTo = "experience"
            });
        }

        return evidence.Where(x => !string.IsNullOrWhiteSpace(x.Quote)).ToList();
    }

    private static List<object> BuildCompetencyAssessment(Dictionary<string, double> competencyWeights, double weightedCoverage, List<AiEvidenceSnippet> evidence)
    {
        var defaults = competencyWeights.Count == 0
            ? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["technical"] = 1
            }
            : competencyWeights;

        return defaults.Select(x => new
        {
            key = x.Key,
            score = Math.Clamp((int)Math.Round(weightedCoverage * 5, MidpointRounding.AwayFromZero), 0, 5),
            rationale = $"Assessment weighted by '{x.Key}' factor ({x.Value:0.##}).",
            evidence_quotes = evidence.Take(2).Select(e => new { quote = e.Quote, related_to = e.RelatedTo }).ToList()
        }).ToList<object>();
    }

    private static List<object> BuildStrengths(List<object> matched, List<AiEvidenceSnippet> evidence)
    {
        if (matched.Count == 0 || evidence.Count == 0)
        {
            return new List<object>();
        }

        return
        [
            new
            {
                title = "Skill Alignment",
                detail = $"Profile matches {matched.Count} weighted job skills.",
                evidence_quotes = evidence.Take(2).Select(e => new { quote = e.Quote, related_to = e.RelatedTo }).ToList()
            }
        ];
    }

    private static List<object> BuildRisks(List<string> missingRequired, List<string> missingNice, MockInput input, List<AiEvidenceSnippet> evidence)
    {
        var risks = new List<object>();
        if (evidence.Count == 0)
        {
            return risks;
        }

        if (missingRequired.Count > 0)
        {
            risks.Add(new
            {
                title = "Required Skill Gaps",
                detail = $"Missing required skills: {string.Join(", ", missingRequired)}",
                severity = 4,
                evidence_quotes = evidence.Take(2).Select(e => new { quote = e.Quote, related_to = e.RelatedTo }).ToList()
            });
        }

        if (input.Job.MinExperienceMonths is not null &&
            (input.Profile.TotalExperienceMonths is null || input.Profile.TotalExperienceMonths < input.Job.MinExperienceMonths))
        {
            risks.Add(new
            {
                title = "Experience Gap",
                detail = $"Detected experience ({input.Profile.TotalExperienceMonths?.ToString() ?? "unknown"} months) is below job minimum ({input.Job.MinExperienceMonths} months).",
                severity = 3,
                evidence_quotes = evidence.Take(2).Select(e => new { quote = e.Quote, related_to = e.RelatedTo }).ToList()
            });
        }
        else if (missingNice.Count > 0)
        {
            risks.Add(new
            {
                title = "Nice-To-Have Gaps",
                detail = $"Missing nice-to-have skills: {string.Join(", ", missingNice.Take(5))}",
                severity = 2,
                evidence_quotes = evidence.Take(1).Select(e => new { quote = e.Quote, related_to = e.RelatedTo }).ToList()
            });
        }

        return risks;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private class MockInput
    {
        public MockJob Job { get; set; } = new();
        public MockProfile Profile { get; set; } = new();
        public Dictionary<string, double> CompetencyWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<MockSkillWeight> SkillWeights { get; set; } = new();
    }

    private class MockJob
    {
        public int? MinExperienceMonths { get; set; }
    }

    private class MockProfile
    {
        public string Summary { get; set; } = string.Empty;
        public int? TotalExperienceMonths { get; set; }
        public List<string> Skills { get; set; } = new();
    }

    private class MockSkillWeight
    {
        public string Name { get; set; } = string.Empty;
        public double Weight { get; set; }
        public bool IsRequired { get; set; }
    }

    private class MockInterviewAnalyzeInput
    {
        public List<MockInterviewMessage> Messages { get; set; } = new();
    }

    private class MockInterviewMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class MockInterviewPlanInput
    {
        public AdaptiveAnalyzeOutput Analyze { get; set; } = new();
    }
}
