using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var runMigrationsOnly = args.Any(x => x.Equals("--migrate", StringComparison.OrdinalIgnoreCase));
var builder = WebApplication.CreateBuilder(args);
var authorizationEnforced = bool.TryParse(builder.Configuration["AUTHORIZATION_ENFORCED"], out var authorizationFromEnv)
    ? authorizationFromEnv
    : builder.Configuration.GetValue<bool?>("Authorization:Enforced") ?? !builder.Environment.IsDevelopment();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IK Otomasyon API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        var configured = builder.Configuration["CORS_ALLOWED_ORIGINS"]
            ?? builder.Configuration["Cors:AllowedOrigins"];
        var origins = (configured ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (origins.Count == 0 && builder.Environment.IsDevelopment())
        {
            origins =
            [
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173"
            ];
        }

        if (origins.Count > 0)
        {
            policy.WithOrigins(origins.ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var rateLimitEnabled = bool.TryParse(builder.Configuration["RATE_LIMIT_ENABLED"], out var rateLimitFromEnv)
    ? rateLimitFromEnv
    : builder.Configuration.GetValue<bool?>("RateLimit:Enabled") ?? !builder.Environment.IsDevelopment();
var globalLimitPerMinute = builder.Configuration.GetValue<int?>("RateLimit:GlobalPerMinute") ?? 60;
var strictLimitPerMinute = builder.Configuration.GetValue<int?>("RateLimit:StrictPerMinute") ?? 10;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please retry later."
        });
        await context.HttpContext.Response.WriteAsync(payload, ct);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (!rateLimitEnabled)
        {
            return RateLimitPartition.GetNoLimiter("rate-limit-disabled");
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isStrictPath = Program.IsStrictRateLimitedPath(path);
        var key = $"{(isStrictPath ? "strict" : "global")}:{Program.ResolveRateLimitIdentity(httpContext, isStrictPath)}";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isStrictPath ? strictLimitPerMinute : globalLimitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0
            });
    });
});

builder.Services.Configure<JwtOptions>(opts =>
{
    var section = builder.Configuration.GetSection(JwtOptions.SectionName);
    opts.Issuer = builder.Configuration["JWT_ISSUER"]
        ?? section[nameof(JwtOptions.Issuer)]
        ?? opts.Issuer;
    opts.Audience = builder.Configuration["JWT_AUDIENCE"]
        ?? section[nameof(JwtOptions.Audience)]
        ?? opts.Audience;
    opts.Secret = builder.Configuration["JWT_SECRET"]
        ?? section[nameof(JwtOptions.Secret)]
        ?? opts.Secret;
    opts.AccessTokenMinutes = int.TryParse(builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"], out var accessTokenMinutes)
        ? accessTokenMinutes
        : section.GetValue<int?>(nameof(JwtOptions.AccessTokenMinutes)) ?? opts.AccessTokenMinutes;
    opts.RefreshTokenDays = int.TryParse(builder.Configuration["JWT_REFRESH_TOKEN_DAYS"], out var refreshTokenDays)
        ? refreshTokenDays
        : section.GetValue<int?>(nameof(JwtOptions.RefreshTokenDays)) ?? opts.RefreshTokenDays;
});
builder.Services.Configure<CvStorageOptions>(builder.Configuration.GetSection(CvStorageOptions.SectionName));
builder.Services.Configure<FormOptions>(opts =>
{
    var maxUploadBytes = builder.Configuration.GetValue<long?>($"{CvStorageOptions.SectionName}:MaxFileSizeBytes")
        ?? 10 * 1024 * 1024;
    opts.MultipartBodyLengthLimit = maxUploadBytes;
});
builder.Services.Configure<AuditOptions>(opts =>
{
    opts.LogUnauthorized = bool.TryParse(builder.Configuration["AUDIT_LOG_UNAUTHORIZED"], out var logUnauthorized)
        ? logUnauthorized
        : builder.Configuration.GetValue<bool?>($"{AuditOptions.SectionName}:LogUnauthorized") ?? true;
});
builder.Services.Configure<ScoringOptions>(opts =>
{
    opts.Mode = builder.Configuration["SCORING_MODE"]
        ?? builder.Configuration[$"{ScoringOptions.SectionName}:Mode"]
        ?? "deterministic";
    opts.LlmScoringEnabled = bool.TryParse(builder.Configuration["LLM_SCORING_ENABLED"], out var llmScoringEnabled)
        ? llmScoringEnabled
        : builder.Configuration.GetValue<bool?>($"{ScoringOptions.SectionName}:LlmScoringEnabled") ?? false;
});
builder.Services.Configure<AdaptiveInterviewOptions>(opts =>
{
    opts.Enabled = bool.TryParse(builder.Configuration["ADAPTIVE_INTERVIEW_ENABLED"], out var adaptiveEnabled)
        ? adaptiveEnabled
        : builder.Configuration.GetValue<bool?>($"{AdaptiveInterviewOptions.SectionName}:Enabled") ?? false;
    opts.LastMessageWindow = int.TryParse(builder.Configuration["ADAPTIVE_LAST_MESSAGES"], out var n)
        ? n
        : builder.Configuration.GetValue<int?>($"{AdaptiveInterviewOptions.SectionName}:LastMessageWindow") ?? 8;
    opts.PlanHorizonMin = builder.Configuration.GetValue<int?>($"{AdaptiveInterviewOptions.SectionName}:PlanHorizonMin") ?? 2;
    opts.PlanHorizonMax = builder.Configuration.GetValue<int?>($"{AdaptiveInterviewOptions.SectionName}:PlanHorizonMax") ?? 3;
});
builder.Services.Configure<LlmOptions>(opts =>
{
    opts.Enabled = bool.TryParse(builder.Configuration["LLM_ENABLED"], out var llmEnabled)
        ? llmEnabled
        : builder.Configuration.GetValue<bool?>($"{LlmOptions.SectionName}:Enabled") ?? false;
    opts.Provider = builder.Configuration["LLM_PROVIDER"]
        ?? builder.Configuration[$"{LlmOptions.SectionName}:Provider"]
        ?? "openai";
    opts.ApiKey = builder.Configuration["LLM_API_KEY"]
        ?? builder.Configuration[$"{LlmOptions.SectionName}:ApiKey"]
        ?? string.Empty;
    opts.BaseUrl = builder.Configuration["LLM_BASE_URL"]
        ?? builder.Configuration[$"{LlmOptions.SectionName}:BaseUrl"];
    opts.Model = builder.Configuration["LLM_MODEL"]
        ?? builder.Configuration[$"{LlmOptions.SectionName}:Model"]
        ?? "mock-llm-v1";
    opts.TimeoutSeconds = int.TryParse(builder.Configuration["LLM_TIMEOUT_SECONDS"], out var timeoutSeconds)
        ? timeoutSeconds
        : builder.Configuration.GetValue<int?>($"{LlmOptions.SectionName}:TimeoutSeconds") ?? 20;
});
var jwtOptions = new JwtOptions
{
    Issuer = builder.Configuration["JWT_ISSUER"]
        ?? builder.Configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)}"]
        ?? "ik-otomasyon",
    Audience = builder.Configuration["JWT_AUDIENCE"]
        ?? builder.Configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)}"]
        ?? "ik-otomasyon",
    Secret = builder.Configuration["JWT_SECRET"]
        ?? builder.Configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.Secret)}"]
        ?? string.Empty,
    AccessTokenMinutes = int.TryParse(builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"], out var accessTokenMinutes)
        ? accessTokenMinutes
        : builder.Configuration.GetValue<int?>($"{JwtOptions.SectionName}:{nameof(JwtOptions.AccessTokenMinutes)}") ?? 15,
    RefreshTokenDays = int.TryParse(builder.Configuration["JWT_REFRESH_TOKEN_DAYS"], out var refreshTokenDays)
        ? refreshTokenDays
        : builder.Configuration.GetValue<int?>($"{JwtOptions.SectionName}:{nameof(JwtOptions.RefreshTokenDays)}") ?? 7
};
jwtOptions.Secret = ResolveJwtSecret(builder.Environment, jwtOptions.Secret, builder.Configuration);
builder.Services.PostConfigure<JwtOptions>(opts =>
{
    opts.Issuer = jwtOptions.Issuer;
    opts.Audience = jwtOptions.Audience;
    opts.Secret = jwtOptions.Secret;
    opts.AccessTokenMinutes = jwtOptions.AccessTokenMinutes;
    opts.RefreshTokenDays = jwtOptions.RefreshTokenDays;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<AppMetrics>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AtsWorkflowService>();
builder.Services.AddScoped<CvStorageService>();
builder.Services.AddScoped<CvParserService>();
builder.Services.AddScoped<CvWorkflowService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<AiEvaluationService>();
builder.Services.AddScoped<RubricService>();
builder.Services.AddScoped<InterviewScoringEnrichmentService>();
builder.Services.AddScoped<InterviewScoringService>();
builder.Services.AddScoped<InterviewOrchestratorService>();
builder.Services.AddHttpClient<RealLLMClient>();
builder.Services.AddScoped<ILLMClient>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
    return options.Enabled
        ? sp.GetRequiredService<RealLLMClient>()
        : new MockLLMClient();
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();
builder.Services.AddSingleton(new AuthorizationRuntimeState
{
    Enforced = authorizationEnforced
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ResourcePolicies.CanReadJob, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanReadJob)));
    options.AddPolicy(ResourcePolicies.CanManageJob, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanManageJob)));
    options.AddPolicy(ResourcePolicies.CanReadCandidate, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanReadCandidate)));
    options.AddPolicy(ResourcePolicies.CanEditCandidate, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanEditCandidate)));
    options.AddPolicy(ResourcePolicies.CanApplyApplication, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanApplyApplication)));
    options.AddPolicy(ResourcePolicies.CanReadApplication, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanReadApplication)));
    options.AddPolicy(ResourcePolicies.CanManageApplication, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanManageApplication)));
    options.AddPolicy(ResourcePolicies.CanAccessInterview, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanAccessInterview)));
    options.AddPolicy(ResourcePolicies.CanAccessScorecard, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanAccessScorecard)));
    options.AddPolicy(ResourcePolicies.CanOverrideScorecard, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanOverrideScorecard)));
    options.AddPolicy(ResourcePolicies.CanRunAiEvaluation, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanRunAiEvaluation)));
    options.AddPolicy(ResourcePolicies.CanParseCvDocument, policy => policy.AddRequirements(new ResourceAuthorizationRequirement(ResourcePolicies.CanParseCvDocument)));

    if (authorizationEnforced)
    {
        return;
    }

    var allowAllPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true)
        .Build();

    options.DefaultPolicy = allowAllPolicy;
    options.FallbackPolicy = allowAllPolicy;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var applyMigrationsOnStartup = bool.TryParse(app.Configuration["APPLY_MIGRATIONS_ON_STARTUP"], out var migrateFromEnv)
        ? migrateFromEnv
        : app.Configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup") ?? !env.IsProduction();
    await DbSeeder.SeedAsync(db, env, applyMigrationsOnStartup);

    var authorizationState = scope.ServiceProvider.GetRequiredService<AuthorizationRuntimeState>();
    if (!authorizationState.Enforced)
    {
        authorizationState.BypassUserId = await db.Users
            .AsNoTracking()
            .Where(x => x.Email == "admin@local.test")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }
}

if (runMigrationsOnly)
{
    app.Logger.LogInformation("Migration command completed successfully.");
    return;
}

app.UseSwagger();
app.UseSwaggerUI();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("ApiCors");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var authorizationState = context.RequestServices.GetRequiredService<AuthorizationRuntimeState>();
    if (!authorizationState.Enforced && context.User.Identity?.IsAuthenticated != true && authorizationState.BypassUserId is Guid bypassUserId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, bypassUserId.ToString()),
            new Claim("sub", bypassUserId.ToString())
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AuthorizationBypass"));
    }

    await next();
});
app.UseRateLimiter();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program
{
    internal static string ResolveJwtSecret(IWebHostEnvironment environment, string? configuredSecret, IConfiguration configuration)
    {
        var secret = configuredSecret?.Trim() ?? string.Empty;
        var hasSecret = !string.IsNullOrWhiteSpace(secret)
            && !secret.Equals("REQUIRED_FROM_ENV", StringComparison.OrdinalIgnoreCase);

        if (environment.IsProduction())
        {
            var envSecret = configuration["JWT_SECRET"]?.Trim();
            if (string.IsNullOrWhiteSpace(envSecret) || envSecret.Length < 32)
            {
                throw new InvalidOperationException("JWT_SECRET is required in Production and must be at least 32 characters.");
            }

            return envSecret;
        }

        if (hasSecret && secret.Length >= 32)
        {
            return secret;
        }

        return "dev-only-jwt-secret-not-for-production-0000";
    }

    internal static bool IsStrictRateLimitedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var lower = path.ToLowerInvariant();
        return lower.StartsWith("/cv/", StringComparison.Ordinal)
            || lower.Contains("/ai-evaluate", StringComparison.Ordinal)
            || (lower.Contains("/interviews/", StringComparison.Ordinal) && lower.EndsWith("/messages", StringComparison.Ordinal))
            || lower.Contains("/score/auto", StringComparison.Ordinal);
    }

    internal static string ResolveRateLimitIdentity(HttpContext context, bool preferUserId)
    {
        if (preferUserId)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }
        }

        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return $"ip:{first}";
            }
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
