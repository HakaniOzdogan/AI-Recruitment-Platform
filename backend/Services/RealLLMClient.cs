using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace IkOtomasyon.Api.Services;

public class RealLLMClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly ILogger<RealLLMClient> _logger;
    private readonly AppMetrics _metrics;
    private readonly AsyncPolicy _resiliencePolicy;

    public RealLLMClient(HttpClient httpClient, IOptions<LlmOptions> options, ILogger<RealLLMClient> logger, AppMetrics metrics)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _metrics = metrics;

        var timeoutSeconds = Math.Max(5, _options.TimeoutSeconds);
        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds), TimeoutStrategy.Optimistic);
        var circuitPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<OperationCanceledException>()
            .Or<LlmProviderException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogWarning(
                        ex,
                        "LLM circuit breaker opened provider={Provider} model={Model} breakSeconds={BreakSeconds}",
                        _options.Provider,
                        _options.Model,
                        breakDelay.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation(
                        "LLM circuit breaker reset provider={Provider} model={Model}",
                        _options.Provider,
                        _options.Model);
                });
        var bulkheadPolicy = Policy.BulkheadAsync(maxParallelization: 5, maxQueuingActions: 0);

        _resiliencePolicy = Policy.WrapAsync(bulkheadPolicy, circuitPolicy, timeoutPolicy);
    }

    public async Task<string> GenerateStructuredAsync(string systemPrompt, string userPrompt, string jsonSchema, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new LlmProviderException("LLM_API_KEY is required when LLM_ENABLED=true.");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var promptHash = BuildHash(systemPrompt + userPrompt);
        var promptLength = systemPrompt.Length + userPrompt.Length;
        var sw = Stopwatch.StartNew();
        var success = false;

        try
        {
            var firstResponse = await SendRequestAsync(requestId, systemPrompt, userPrompt, ct);
            var firstValidation = StructuredJsonValidator.Validate(firstResponse, jsonSchema);
            if (firstValidation.IsValid)
            {
                success = true;
                return firstResponse;
            }

            var repairPrompt = BuildRepairPrompt(jsonSchema, firstResponse);
            var repaired = await SendRequestAsync(requestId, systemPrompt, repairPrompt, ct);
            var repairedValidation = StructuredJsonValidator.Validate(repaired, jsonSchema);
            if (repairedValidation.IsValid)
            {
                success = true;
                return repaired;
            }

            var summary = string.Join("; ", repairedValidation.Errors.Take(5).Select(x => $"{x.Path}: {x.Message}"));
            throw new LlmSchemaException($"LLM structured output validation failed after retry: {summary}");
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LlmTimeoutException($"LLM request timed out after {_options.TimeoutSeconds} seconds.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new LlmProviderException("LLM circuit breaker is open. Falling back to deterministic flow.", ex);
        }
        catch (BulkheadRejectedException ex)
        {
            throw new LlmProviderException("LLM concurrency limit reached. Falling back to deterministic flow.", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new LlmTimeoutException($"LLM request timed out after {_options.TimeoutSeconds} seconds.", ex);
        }
        catch (LlmSchemaException)
        {
            throw;
        }
        catch (LlmTimeoutException)
        {
            throw;
        }
        catch (LlmProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LlmProviderException($"LLM provider call failed: {ex.Message}", ex);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "LLM request completed. requestId={RequestId} provider={Provider} model={Model} latencyMs={LatencyMs} promptHash={PromptHash} promptLength={PromptLength}",
                requestId,
                _options.Provider,
                _options.Model,
                sw.ElapsedMilliseconds,
                promptHash,
                promptLength);
            _metrics.RecordLlmCall(_options.Provider, _options.Model, success, sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<string> SendRequestAsync(string requestId, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(
            async token => await SendRequestCoreAsync(requestId, systemPrompt, userPrompt, token),
            ct);
    }

    private async Task<string> SendRequestCoreAsync(string requestId, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var payload = BuildProviderPayload(systemPrompt, userPrompt);
        using var message = new HttpRequestMessage(HttpMethod.Post, BuildProviderUrl())
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        ApplyProviderHeaders(message);
        message.Headers.Add("x-request-id", requestId);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new LlmProviderException($"LLM provider returned {(int)response.StatusCode}.");
        }

        return ExtractContent(body);
    }

    private string BuildProviderUrl()
    {
        var provider = _options.Provider.Trim().ToLowerInvariant();
        var baseUrl = _options.BaseUrl?.Trim().TrimEnd('/');
        return provider switch
        {
            "openai" => $"{(string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com" : baseUrl)}/v1/chat/completions",
            "azure_openai" => BuildAzureUrl(baseUrl),
            _ => throw new LlmProviderException($"Unsupported LLM provider '{_options.Provider}'.")
        };
    }

    private string BuildAzureUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new LlmProviderException("LLM_BASE_URL is required for azure_openai provider.");
        }

        var deployment = Uri.EscapeDataString(_options.Model);
        return $"{baseUrl}/openai/deployments/{deployment}/chat/completions?api-version=2024-08-01-preview";
    }

    private void ApplyProviderHeaders(HttpRequestMessage request)
    {
        var provider = _options.Provider.Trim().ToLowerInvariant();
        if (provider == "openai")
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            return;
        }

        if (provider == "azure_openai")
        {
            request.Headers.Add("api-key", _options.ApiKey);
            return;
        }

        throw new LlmProviderException($"Unsupported LLM provider '{_options.Provider}'.");
    }

    private string BuildProviderPayload(string systemPrompt, string userPrompt)
    {
        var provider = _options.Provider.Trim().ToLowerInvariant();
        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };

        if (provider == "openai")
        {
            var payload = new
            {
                model = _options.Model,
                temperature = 0.1,
                messages,
                response_format = new { type = "json_object" }
            };
            return JsonSerializer.Serialize(payload);
        }

        if (provider == "azure_openai")
        {
            var payload = new
            {
                temperature = 0.1,
                messages,
                response_format = new { type = "json_object" }
            };
            return JsonSerializer.Serialize(payload);
        }

        throw new LlmProviderException($"Unsupported LLM provider '{_options.Provider}'.");
    }

    private static string ExtractContent(string providerResponse)
    {
        using var doc = JsonDocument.Parse(providerResponse);
        if (!doc.RootElement.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            throw new LlmProviderException("LLM response missing choices.");
        }

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var message))
        {
            throw new LlmProviderException("LLM response missing choices[0].message.");
        }

        if (!message.TryGetProperty("content", out var content))
        {
            throw new LlmProviderException("LLM response missing choices[0].message.content.");
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return EnsureJson(content.GetString() ?? string.Empty);
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var joined = string.Join(" ", content.EnumerateArray()
                .Where(x => x.TryGetProperty("text", out _))
                .Select(x => x.GetProperty("text").GetString() ?? string.Empty));
            return EnsureJson(joined);
        }

        throw new LlmProviderException("LLM response content format is not supported.");
    }

    private static string EnsureJson(string content)
    {
        var clean = content.Trim();
        if (clean.StartsWith("```", StringComparison.Ordinal))
        {
            clean = clean.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        var firstBrace = clean.IndexOf('{');
        var lastBrace = clean.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            clean = clean[firstBrace..(lastBrace + 1)];
        }

        return clean;
    }

    private static string BuildRepairPrompt(string jsonSchema, string invalidJson)
    {
        return $"""
Return corrected JSON only.
Rules:
- Must be valid JSON.
- Must follow this schema contract exactly:
{jsonSchema}
- Return ONLY valid JSON matching schema.

Previous invalid output:
{invalidJson}
""";
    }

    private static string BuildHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
