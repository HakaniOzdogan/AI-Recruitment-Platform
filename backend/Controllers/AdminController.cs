using BCrypt.Net;
using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    [RequirePermission(PermissionKeys.UserManage)]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> ListUsers(CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);

        return Ok(users.Select(ToAdminUserResponse));
    }

    [HttpPost("users")]
    [RequirePermission(PermissionKeys.UserManage)]
    public async Task<ActionResult<AdminUserResponse>> CreateUser([FromBody] AdminUserCreateRequest request, CancellationToken ct)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { message = "fullName, email ve password zorunlu." });
        }

        if (password.Length < 8)
        {
            return BadRequest(new { message = "Password en az 8 karakter olmali." });
        }

        var emailExists = await _db.Users.AnyAsync(x => x.Email.ToLower() == email, ct);
        if (emailExists)
        {
            return Conflict(new { message = "Bu email ile kullanici zaten var." });
        }

        var roles = await _db.Roles
            .Where(x => request.RoleIds.Contains(x.Id))
            .ToListAsync(ct);

        if (roles.Count != request.RoleIds.Distinct().Count())
        {
            return BadRequest(new { message = "Gecersiz role id bulundu." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        foreach (var role in roles)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _db.SaveChangesAsync(ct);

        var created = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstAsync(x => x.Id == user.Id, ct);

        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, ToAdminUserResponse(created));
    }

    [HttpGet("users/{userId:guid}")]
    [RequirePermission(PermissionKeys.UserManage)]
    public async Task<ActionResult<AdminUserResponse>> GetUserById(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToAdminUserResponse(user));
    }

    [HttpPut("users/{userId:guid}")]
    [RequirePermission(PermissionKeys.UserManage)]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(Guid userId, [FromBody] AdminUserUpdateRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
        {
            return NotFound();
        }

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "fullName ve email zorunlu." });
        }

        var emailExists = await _db.Users.AnyAsync(x => x.Id != userId && x.Email.ToLower() == email, ct);
        if (emailExists)
        {
            return Conflict(new { message = "Bu email baska bir kullanicida mevcut." });
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        var roles = await _db.Roles.Where(x => roleIds.Contains(x.Id)).ToListAsync(ct);
        if (roles.Count != roleIds.Count)
        {
            return BadRequest(new { message = "Gecersiz role id bulundu." });
        }

        user.FullName = fullName;
        user.Email = email;
        user.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Trim().Length < 8)
            {
                return BadRequest(new { message = "Password en az 8 karakter olmali." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());
        }

        _db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var role in roles)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.Id
            });
        }

        await _db.SaveChangesAsync(ct);

        var updated = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstAsync(x => x.Id == userId, ct);

        return Ok(ToAdminUserResponse(updated));
    }

    [HttpDelete("users/{userId:guid}")]
    [RequirePermission(PermissionKeys.UserManage)]
    public async Task<ActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        var actorId = User.TryGetUserId();
        if (actorId == userId)
        {
            return BadRequest(new { message = "Kendi kullanicinizi silemezsiniz." });
        }

        var user = await _db.Users
            .Include(x => x.UserRoles)
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return NotFound();
        }

        _db.UserRoles.RemoveRange(user.UserRoles);
        _db.RefreshTokens.RemoveRange(user.RefreshTokens);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("roles")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<IEnumerable<AdminRoleResponse>>> ListRoles(CancellationToken ct)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(roles.Select(ToAdminRoleResponse));
    }

    [HttpPost("roles")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminRoleResponse>> CreateRole([FromBody] AdminRoleUpsertRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Role name zorunlu." });
        }

        var exists = await _db.Roles.AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);
        if (exists)
        {
            return Conflict(new { message = "Bu role zaten mevcut." });
        }

        var permissionKeys = request.PermissionKeys
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var permissions = await _db.Permissions
            .Where(x => permissionKeys.Contains(x.Key))
            .ToListAsync(ct);
        if (permissions.Count != permissionKeys.Count)
        {
            return BadRequest(new { message = "Gecersiz permission key bulundu." });
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        _db.Roles.Add(role);
        foreach (var permission in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(ct);

        var created = await _db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles)
            .FirstAsync(x => x.Id == role.Id, ct);

        return CreatedAtAction(nameof(GetRoleById), new { roleId = role.Id }, ToAdminRoleResponse(created));
    }

    [HttpGet("roles/{roleId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminRoleResponse>> GetRoleById(Guid roleId, CancellationToken ct)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == roleId, ct);

        if (role is null)
        {
            return NotFound();
        }

        return Ok(ToAdminRoleResponse(role));
    }

    [HttpPut("roles/{roleId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminRoleResponse>> UpdateRole(Guid roleId, [FromBody] AdminRoleUpsertRequest request, CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(x => x.RolePermissions)
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == roleId, ct);
        if (role is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Role name zorunlu." });
        }

        var exists = await _db.Roles.AnyAsync(x => x.Id != roleId && x.Name.ToLower() == name.ToLower(), ct);
        if (exists)
        {
            return Conflict(new { message = "Bu role adı zaten mevcut." });
        }

        var permissionKeys = request.PermissionKeys
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var permissions = await _db.Permissions
            .Where(x => permissionKeys.Contains(x.Key))
            .ToListAsync(ct);
        if (permissions.Count != permissionKeys.Count)
        {
            return BadRequest(new { message = "Gecersiz permission key bulundu." });
        }

        role.Name = name;
        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permission in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(ct);

        var updated = await _db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles)
            .FirstAsync(x => x.Id == roleId, ct);

        return Ok(ToAdminRoleResponse(updated));
    }

    [HttpDelete("roles/{roleId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult> DeleteRole(Guid roleId, CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(x => x.UserRoles)
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == roleId, ct);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Admin role silinemez." });
        }

        _db.UserRoles.RemoveRange(role.UserRoles);
        _db.RolePermissions.RemoveRange(role.RolePermissions);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("pipeline-stages")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<IEnumerable<AdminPipelineStageResponse>>> ListPipelineStages(CancellationToken ct)
    {
        var stages = await _db.PipelineStages
            .AsNoTracking()
            .OrderBy(x => x.Order)
            .Select(x => new AdminPipelineStageResponse(
                x.Id,
                x.Name,
                x.Order,
                x.IsTerminal,
                x.Applications.Count))
            .ToListAsync(ct);

        return Ok(stages);
    }

    [HttpPost("pipeline-stages")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminPipelineStageResponse>> CreatePipelineStage(
        [FromBody] AdminPipelineStageUpsertRequest request,
        CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Stage name zorunlu." });
        }

        if (request.Order <= 0)
        {
            return BadRequest(new { message = "Order pozitif olmali." });
        }

        var existsByName = await _db.PipelineStages.AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct);
        if (existsByName)
        {
            return Conflict(new { message = "Bu isimde stage zaten var." });
        }

        var existsByOrder = await _db.PipelineStages.AnyAsync(x => x.Order == request.Order, ct);
        if (existsByOrder)
        {
            return Conflict(new { message = "Bu order degerinde stage zaten var." });
        }

        var stage = new PipelineStage
        {
            Id = Guid.NewGuid(),
            Name = name,
            Order = request.Order,
            IsTerminal = request.IsTerminal
        };

        _db.PipelineStages.Add(stage);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPipelineStageById), new { stageId = stage.Id }, new AdminPipelineStageResponse(
            stage.Id,
            stage.Name,
            stage.Order,
            stage.IsTerminal,
            0));
    }

    [HttpGet("pipeline-stages/{stageId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminPipelineStageResponse>> GetPipelineStageById(Guid stageId, CancellationToken ct)
    {
        var stage = await _db.PipelineStages
            .AsNoTracking()
            .Where(x => x.Id == stageId)
            .Select(x => new AdminPipelineStageResponse(
                x.Id,
                x.Name,
                x.Order,
                x.IsTerminal,
                x.Applications.Count))
            .FirstOrDefaultAsync(ct);

        if (stage is null)
        {
            return NotFound();
        }

        return Ok(stage);
    }

    [HttpPut("pipeline-stages/{stageId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult<AdminPipelineStageResponse>> UpdatePipelineStage(
        Guid stageId,
        [FromBody] AdminPipelineStageUpsertRequest request,
        CancellationToken ct)
    {
        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId, ct);
        if (stage is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Stage name zorunlu." });
        }

        if (request.Order <= 0)
        {
            return BadRequest(new { message = "Order pozitif olmali." });
        }

        var existsByName = await _db.PipelineStages.AnyAsync(x => x.Id != stageId && x.Name.ToLower() == name.ToLower(), ct);
        if (existsByName)
        {
            return Conflict(new { message = "Bu isimde stage zaten var." });
        }

        var existsByOrder = await _db.PipelineStages.AnyAsync(x => x.Id != stageId && x.Order == request.Order, ct);
        if (existsByOrder)
        {
            return Conflict(new { message = "Bu order degerinde stage zaten var." });
        }

        stage.Name = name;
        stage.Order = request.Order;
        stage.IsTerminal = request.IsTerminal;

        await _db.SaveChangesAsync(ct);

        var applicationCount = await _db.Applications.CountAsync(x => x.StageId == stageId, ct);
        return Ok(new AdminPipelineStageResponse(stage.Id, stage.Name, stage.Order, stage.IsTerminal, applicationCount));
    }

    [HttpDelete("pipeline-stages/{stageId:guid}")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public async Task<ActionResult> DeletePipelineStage(Guid stageId, CancellationToken ct)
    {
        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId, ct);
        if (stage is null)
        {
            return NotFound();
        }

        var applicationCount = await _db.Applications.CountAsync(x => x.StageId == stageId, ct);
        if (applicationCount > 0)
        {
            return Conflict(new { message = "Bu stage aktif applicationlarda kullaniliyor." });
        }

        var totalCount = await _db.PipelineStages.CountAsync(ct);
        if (totalCount <= 1)
        {
            return BadRequest(new { message = "En az bir pipeline stage kalmali." });
        }

        _db.PipelineStages.Remove(stage);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("permissions")]
    [RequirePermission(PermissionKeys.RoleManage)]
    public ActionResult<IEnumerable<string>> ListPermissions()
    {
        return Ok(PermissionKeys.All);
    }

    private static AdminUserResponse ToAdminUserResponse(User user)
    {
        var roles = user.UserRoles
            .Select(x => x.Role?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .OrderBy(x => x)
            .ToList();
        return new AdminUserResponse(user.Id, user.FullName, user.Email, user.Status, user.CreatedAt, roles);
    }

    private static AdminRoleResponse ToAdminRoleResponse(Role role)
    {
        var permissions = role.RolePermissions
            .Select(x => x.Permission?.Key)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .OrderBy(x => x)
            .ToList();

        return new AdminRoleResponse(role.Id, role.Name, permissions, role.UserRoles.Count);
    }
}
