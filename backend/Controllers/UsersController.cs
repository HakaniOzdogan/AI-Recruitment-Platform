using System.Security.Claims;
using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Route("")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Status,
            Roles = roles
        });
    }

    [Authorize]
    [RequirePermission(PermissionKeys.UserManage)]
    [HttpGet("admin/users/summary")]
    public async Task<ActionResult<IEnumerable<UserSummary>>> ListUsers(CancellationToken ct)
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummary(u.Id, u.FullName, u.Email, u.Status))
            .ToListAsync(ct);

        return Ok(users);
    }
}
