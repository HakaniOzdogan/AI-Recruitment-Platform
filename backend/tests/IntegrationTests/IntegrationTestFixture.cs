using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BCrypt.Net;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace IkOtomasyon.Api.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ik_otomasyon_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IntegrationTestWebAppFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = new IntegrationTestWebAppFactory(_postgres.GetConnectionString());
        Client = Factory.CreateClient();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.EnsureDeletedAsync();
        await DbSeeder.SeedAsync(db, env);
    }

    public async Task<(Guid UserId, string Email, string Password)> EnsureUserAsync(string roleName, string? emailPrefix = null, Guid? tenantId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = await db.Roles.FirstAsync(x => x.Name == roleName);

        var email = $"{emailPrefix ?? roleName.ToLowerInvariant()}.{Guid.NewGuid():N}@test.local";
        const string password = "Admin123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FullName = $"{roleName} User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        return (user.Id, email, password);
    }

    public async Task<string> GetJwtAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    public async Task<Guid> CreateInterviewSessionAsync(Guid applicationId, InterviewSessionStatus status)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = new InterviewSession
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = status == InterviewSessionStatus.Completed ? DateTime.UtcNow : null
        };

        db.InterviewSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    public async Task AddInterviewMessageAsync(Guid sessionId, string role, string content)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InterviewMessages.Add(new InterviewMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<InterviewMessage>> GetInterviewMessagesAsync(Guid sessionId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.InterviewMessages
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(string method, string path)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.Method == method && x.Path == path)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public static void SetBearer(HttpClient client, string jwt)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
    }
}
