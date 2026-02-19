using IkOtomasyon.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IkOtomasyon.Api.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public IntegrationTestWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["AUDIT_LOG_UNAUTHORIZED"] = "true",
                ["LLM_ENABLED"] = "false",
                ["ADAPTIVE_INTERVIEW_ENABLED"] = "true",
                ["SCORING_MODE"] = "deterministic",
                ["LLM_SCORING_ENABLED"] = "false",
                ["APPLY_MIGRATIONS_ON_STARTUP"] = "true",
                ["RATE_LIMIT_ENABLED"] = "true",
                ["RateLimit:GlobalPerMinute"] = "120",
                ["RateLimit:StrictPerMinute"] = "10"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
        return base.CreateHost(builder);
    }
}
