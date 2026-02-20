using BCrypt.Net;
using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IWebHostEnvironment env, bool applyMigrationsOnStartup = true)
    {
        if (applyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync();
        }

        var permissions = await EnsurePermissionsAsync(db);
        var roles = await EnsureRolesAsync(db, permissions);
        await EnsurePipelineStagesAsync(db);
        await EnsureDefaultRubricAsync(db);
        await EnsureQuestionBankAsync(db);

        if (env.IsDevelopment())
        {
            await EnsureAdminUserAsync(db, roles[RoleKeys.Admin]);
        }
    }

    private static async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Key);
        foreach (var key in PermissionKeys.All)
        {
            if (existing.ContainsKey(key))
            {
                continue;
            }

            var permission = new Permission { Id = Guid.NewGuid(), Key = key };
            db.Permissions.Add(permission);
            existing[key] = permission;
        }

        await db.SaveChangesAsync();
        return existing;
    }

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Permission> permissions)
    {
        var roleNames = new[]
        {
            RoleKeys.Admin,
            RoleKeys.Recruiter,
            RoleKeys.HiringManager,
            RoleKeys.Interviewer,
            RoleKeys.Applicant,
            RoleKeys.LegacyHr,
            RoleKeys.LegacyUser
        };
        var existing = await db.Roles.ToDictionaryAsync(r => r.Name);

        foreach (var name in roleNames)
        {
            if (existing.ContainsKey(name))
            {
                continue;
            }

            var role = new Role { Id = Guid.NewGuid(), Name = name };
            db.Roles.Add(role);
            existing[name] = role;
        }

        await db.SaveChangesAsync();

        var rolePermissions = new Dictionary<string, string[]>
        {
            [RoleKeys.Admin] = PermissionKeys.All,
            [RoleKeys.Recruiter] =
            [
                PermissionKeys.JobRead,
                PermissionKeys.JobCreate,
                PermissionKeys.JobUpdate,
                PermissionKeys.JobPublish,
                PermissionKeys.JobWeightsRead,
                PermissionKeys.JobWeightsUpdate,
                PermissionKeys.CandidateRead,
                PermissionKeys.CandidateCreate,
                PermissionKeys.CandidateUpdate,
                PermissionKeys.CandidateCvUpload,
                PermissionKeys.CandidateCvParse,
                PermissionKeys.ApplicationCreate,
                PermissionKeys.ApplicationRead,
                PermissionKeys.ApplicationUpdateStage,
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewCreate,
                PermissionKeys.InterviewMessageSend,
                PermissionKeys.ScorecardRead,
                PermissionKeys.ScorecardRunAuto,
                PermissionKeys.ScorecardOverride,
                PermissionKeys.AiEvaluationRead,
                PermissionKeys.AiEvaluationRun,
                PermissionKeys.AuditRead
            ],
            [RoleKeys.HiringManager] =
            [
                PermissionKeys.JobRead,
                PermissionKeys.CandidateRead,
                PermissionKeys.ApplicationRead,
                PermissionKeys.ApplicationUpdateStage,
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewCreate,
                PermissionKeys.ScorecardRead,
                PermissionKeys.ScorecardOverride,
                PermissionKeys.AiEvaluationRead
            ],
            [RoleKeys.Interviewer] =
            [
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewMessageSend,
                PermissionKeys.ScorecardRead
            ],
            [RoleKeys.Applicant] =
            [
                PermissionKeys.JobRead,
                PermissionKeys.CandidateRead,
                PermissionKeys.CandidateCreate,
                PermissionKeys.CandidateUpdate,
                PermissionKeys.CandidateCvUpload,
                PermissionKeys.CandidateCvParse,
                PermissionKeys.ApplicationCreate,
                PermissionKeys.ApplicationRead,
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewMessageSend,
                PermissionKeys.ScorecardRead
            ],
            [RoleKeys.LegacyHr] =
            [
                PermissionKeys.JobRead,
                PermissionKeys.JobCreate,
                PermissionKeys.JobUpdate,
                PermissionKeys.JobPublish,
                PermissionKeys.JobWeightsRead,
                PermissionKeys.JobWeightsUpdate,
                PermissionKeys.CandidateRead,
                PermissionKeys.CandidateCreate,
                PermissionKeys.CandidateUpdate,
                PermissionKeys.CandidateCvUpload,
                PermissionKeys.CandidateCvParse,
                PermissionKeys.ApplicationCreate,
                PermissionKeys.ApplicationRead,
                PermissionKeys.ApplicationUpdateStage,
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewCreate,
                PermissionKeys.InterviewMessageSend,
                PermissionKeys.ScorecardRead,
                PermissionKeys.ScorecardRunAuto,
                PermissionKeys.ScorecardOverride,
                PermissionKeys.AiEvaluationRead,
                PermissionKeys.AiEvaluationRun,
                PermissionKeys.AuditRead
            ],
            [RoleKeys.LegacyUser] =
            [
                PermissionKeys.JobRead,
                PermissionKeys.CandidateRead,
                PermissionKeys.CandidateCreate,
                PermissionKeys.CandidateUpdate,
                PermissionKeys.CandidateCvUpload,
                PermissionKeys.CandidateCvParse,
                PermissionKeys.ApplicationCreate,
                PermissionKeys.ApplicationRead,
                PermissionKeys.InterviewRead,
                PermissionKeys.InterviewMessageSend,
                PermissionKeys.ScorecardRead
            ]
        };

        foreach (var pair in rolePermissions)
        {
            var role = existing[pair.Key];
            foreach (var permissionKey in pair.Value)
            {
                var permission = permissions[permissionKey];
                var exists = await db.RolePermissions.AnyAsync(rp =>
                    rp.RoleId == role.Id && rp.PermissionId == permission.Id);
                if (!exists)
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        await db.SaveChangesAsync();
        return existing;
    }

    private static async Task EnsureAdminUserAsync(AppDbContext db, Role adminRole)
    {
        const string adminEmail = "admin@local.test";
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existing is not null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Local Admin",
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
    }

    private static async Task EnsurePipelineStagesAsync(AppDbContext db)
    {
        var stageDefs = new (int Order, string Name, bool IsTerminal)[]
        {
            (1, "Applied", false),
            (2, "Screen", false),
            (3, "HR Interview", false),
            (4, "Technical Interview", false),
            (5, "Offer", false),
            (6, "Hired", true),
            (7, "Rejected", true)
        };

        var existingByOrder = await db.PipelineStages.ToDictionaryAsync(x => x.Order);
        foreach (var stageDef in stageDefs)
        {
            if (existingByOrder.TryGetValue(stageDef.Order, out var existing))
            {
                existing.Name = stageDef.Name;
                existing.IsTerminal = stageDef.IsTerminal;
                continue;
            }

            db.PipelineStages.Add(new PipelineStage
            {
                Id = Guid.NewGuid(),
                Order = stageDef.Order,
                Name = stageDef.Name,
                IsTerminal = stageDef.IsTerminal
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDefaultRubricAsync(AppDbContext db)
    {
        var template = await db.RubricTemplates
            .Include(x => x.Criteria)
            .FirstOrDefaultAsync(x => x.IsDefault && x.JobId == null);

        if (template is null)
        {
            template = new RubricTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Default Interview Rubric",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            };
            db.RubricTemplates.Add(template);
        }

        var existingByKey = template.Criteria.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < RubricKeys.All.Length; i++)
        {
            var key = RubricKeys.All[i];
            if (existingByKey.TryGetValue(key, out var criterion))
            {
                criterion.Weight = 0.20;
                criterion.Order = i + 1;
                criterion.Title = DefaultTitle(key);
                criterion.Description = DefaultDescription(key);
                continue;
            }

            db.RubricCriteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                Key = key,
                Title = DefaultTitle(key),
                Description = DefaultDescription(key),
                Weight = 0.20,
                Order = i + 1
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureQuestionBankAsync(AppDbContext db)
    {
        if (await db.InterviewQuestionBanks.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var items = new[]
        {
            new InterviewQuestionBank { Id = Guid.NewGuid(), Category = "technical", TopicKey = "sql_indexes", Difficulty = 3, QuestionText = "SQL index stratejisini nasıl seçersin ve yanlış index kullanımını nasıl teşhis edersin?", TagsJson = "[\"sql\",\"performance\"]", IsActive = true, CreatedAt = now },
            new InterviewQuestionBank { Id = Guid.NewGuid(), Category = "technical", TopicKey = "cqrs", Difficulty = 4, QuestionText = "CQRS yaklaşımını hangi senaryolarda kullanırsın, ne zaman kaçınırsın?", TagsJson = "[\"architecture\",\"cqrs\"]", IsActive = true, CreatedAt = now },
            new InterviewQuestionBank { Id = Guid.NewGuid(), Category = "behavioral", TopicKey = "tradeoff", Difficulty = 3, QuestionText = "Zor bir trade-off kararında hangi sinyallere göre karar verdin?", TagsJson = "[\"decision\",\"tradeoff\"]", IsActive = true, CreatedAt = now },
            new InterviewQuestionBank { Id = Guid.NewGuid(), Category = "culture", TopicKey = "ownership", Difficulty = 2, QuestionText = "Sana ait olmayan bir problemi sahiplenip çözdüğün bir örnek paylaşır mısın?", TagsJson = "[\"ownership\",\"team\"]", IsActive = true, CreatedAt = now }
        };

        db.InterviewQuestionBanks.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static string DefaultTitle(string key) => key switch
    {
        RubricKeys.Technical => "Technical",
        RubricKeys.ProblemSolving => "Problem Solving",
        RubricKeys.Communication => "Communication",
        RubricKeys.CultureFit => "Culture Fit",
        RubricKeys.DomainKnowledge => "Domain Knowledge",
        _ => key
    };

    private static string DefaultDescription(string key) => key switch
    {
        RubricKeys.Technical => "Depth and correctness of technical knowledge.",
        RubricKeys.ProblemSolving => "Structured analysis and trade-off reasoning.",
        RubricKeys.Communication => "Clarity, structure, and concise expression.",
        RubricKeys.CultureFit => "Teamwork, ownership, and working style alignment.",
        RubricKeys.DomainKnowledge => "Relevant domain and business context understanding.",
        _ => key
    };
}
