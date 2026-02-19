using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IkOtomasyon.Api.Services;

namespace IkOtomasyon.Api.Tests;

public class RealLlmClientResilienceTests
{
    [Fact]
    public async Task CircuitBreaker_Opens_AfterConsecutiveProviderFailures()
    {
        var handler = new AlwaysFailingHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new LlmOptions
        {
            Enabled = true,
            Provider = "openai",
            ApiKey = "test-key",
            Model = "test-model",
            TimeoutSeconds = 5
        });
        var client = new RealLLMClient(httpClient, options, NullLogger<RealLLMClient>.Instance, new AppMetrics());

        var errors = new List<Exception>();
        for (var i = 0; i < 7; i++)
        {
            try
            {
                await client.GenerateStructuredAsync("system", "user", "{\"foo\":\"bar\"}");
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        Assert.True(errors.Count >= 6);
        Assert.Contains(errors, ex => ex.Message.Contains("circuit breaker is open", StringComparison.OrdinalIgnoreCase));
        Assert.True(handler.CallCount <= 6, $"Expected circuit open to stop outbound calls, actual calls: {handler.CallCount}");
    }

    private sealed class AlwaysFailingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"forced\"}")
            });
        }
    }
}
