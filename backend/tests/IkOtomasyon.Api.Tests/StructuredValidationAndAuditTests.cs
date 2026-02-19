using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Tests;

public class StructuredValidationAndAuditTests
{
    [Fact]
    public void StructuredJsonValidator_StrictRules_FailsOnEnumRange()
    {
        const string planSchema = """
{
  "$schema":"https://json-schema.org/draft/2020-12/schema",
  "type":"object",
  "required":["plan_horizon","planned_questions"],
  "properties":{
    "plan_horizon":{"type":"integer","enum":[2,3]},
    "planned_questions":{
      "type":"array",
      "items":{
        "type":"object",
        "required":["type","category","topic_key","difficulty","question_text","why","guardrails_ok"],
        "properties":{
          "type":{"type":"string"},
          "category":{"type":"string"},
          "topic_key":{"type":"string"},
          "difficulty":{"type":"integer"},
          "question_text":{"type":"string"},
          "why":{"type":"string"},
          "guardrails_ok":{"type":"boolean"}
        }
      }
    }
  }
}
""";

        const string invalid = """
{
  "plan_horizon": 3,
  "planned_questions": [
    {
      "type": "generated",
      "category": "tech",
      "topic_key": "sql_indexes",
      "difficulty": 10,
      "question_text": "x",
      "why": "x",
      "guardrails_ok": true
    }
  ]
}
""";

        var result = StructuredJsonValidator.Validate(invalid, planSchema);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Path.Contains("category", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, x => x.Path.Contains("difficulty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Interviews_Audit_WritesBothGuardrailAndFallback()
    {
        var services = BuildServices(logUnauthorized: true);
        var middleware = new AuditMiddleware(
            _ => Task.CompletedTask,
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditOptions { LogUnauthorized = true }));

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/interviews/abc/messages";
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.AddAuditInfo(AuditActions.LlmGuardrailBlock, "InterviewSession", "abc");
        context.AddAuditInfo(AuditActions.LlmFallbackUsed, "InterviewSession", "abc");

        await middleware.InvokeAsync(context);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actions = await db.AuditLogs.AsNoTracking().Select(x => x.Action).ToListAsync();
        Assert.Contains(AuditActions.LlmGuardrailBlock, actions);
        Assert.Contains(AuditActions.LlmFallbackUsed, actions);
    }

    [Fact]
    public async Task AuditMiddleware_LogsUnauthorizedMutating_WhenEnabled()
    {
        var services = BuildServices(logUnauthorized: true);
        var middleware = new AuditMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            },
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditOptions { LogUnauthorized = true }));

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/jobs/1/publish";

        await middleware.InvokeAsync(context);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(StatusCodes.Status403Forbidden, log!.StatusCode);
        Assert.Equal("POST /jobs/1/publish", log.Action);
    }

    private static ServiceProvider BuildServices(bool logUnauthorized)
    {
        var services = new ServiceCollection();
        var dbRoot = new InMemoryDatabaseRoot();
        var dbName = Guid.NewGuid().ToString("N");
        services.AddSingleton(dbRoot);
        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            opts.UseInMemoryDatabase(dbName, sp.GetRequiredService<InMemoryDatabaseRoot>());
        });
        services.Configure<AuditOptions>(opts => opts.LogUnauthorized = logUnauthorized);
        return services.BuildServiceProvider();
    }
}
