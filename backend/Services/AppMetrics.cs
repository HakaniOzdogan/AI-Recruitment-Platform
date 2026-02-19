using System.Diagnostics.Metrics;

namespace IkOtomasyon.Api.Services;

public sealed class AppMetrics : IDisposable
{
    private readonly Meter _meter = new("IkOtomasyon.Api", "1.0.0");
    private readonly Counter<long> _requestCount;
    private readonly Histogram<double> _requestLatencyMs;
    private readonly Counter<long> _llmCallCount;
    private readonly Histogram<double> _llmLatencyMs;
    private readonly Counter<long> _llmFailureCount;

    public AppMetrics()
    {
        _requestCount = _meter.CreateCounter<long>("request_count");
        _requestLatencyMs = _meter.CreateHistogram<double>("request_latency");
        _llmCallCount = _meter.CreateCounter<long>("llm_call_count");
        _llmLatencyMs = _meter.CreateHistogram<double>("llm_latency");
        _llmFailureCount = _meter.CreateCounter<long>("llm_failures");
    }

    public void RecordRequest(string method, string route, int statusCode, double latencyMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("method", method),
            new("route", route),
            new("status", statusCode)
        };
        _requestCount.Add(1, tags);
        _requestLatencyMs.Record(latencyMs, tags);
    }

    public void RecordLlmCall(string provider, string model, bool success, double latencyMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider", provider),
            new("model", model)
        };
        _llmCallCount.Add(1, tags);
        _llmLatencyMs.Record(latencyMs, tags);
        if (!success)
        {
            _llmFailureCount.Add(1, tags);
        }
    }

    public void Dispose() => _meter.Dispose();
}
