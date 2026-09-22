using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class AdminService(BuildFlowDbContext dbContext) : IAdminService
{
    public async Task<IReadOnlyCollection<AdminUserResponse>> GetUsersAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(user =>
                user.Email.ToLower().Contains(term) ||
                user.FullName.ToLower().Contains(term));
        }

        var users = await query
            .OrderBy(user => user.FullName)
            .Take(250)
            .ToArrayAsync(cancellationToken);
        return users.Select(ToResponse).ToArray();
    }

    public async Task<AdminUserResponse> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var requestedRole = SystemRoles.AssignableByAdministrator.SingleOrDefault(
            item => string.Equals(item, role.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ApiException(
                StatusCodes.Status400BadRequest,
                "invalid_role",
                "Select a valid operational role.");

        var user = await dbContext.Users
            .Include(item => item.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new ApiException(
                StatusCodes.Status404NotFound,
                "user_not_found",
                "The selected user was not found.");

        if (user.UserRoles.Any(userRole => userRole.Role.Name == SystemRoles.Administrator))
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "administrator_role_locked",
                "Administrator roles cannot be changed from user management.");

        if (user.UserRoles.Count == 1 && user.UserRoles.Single().Role.Name == requestedRole)
            return ToResponse(user);

        var targetRole = await dbContext.Roles.SingleAsync(
            item => item.NormalizedName == requestedRole.ToUpperInvariant(),
            cancellationToken);
        var retainedRole = user.UserRoles.SingleOrDefault(userRole => userRole.RoleId == targetRole.Id);
        dbContext.UserRoles.RemoveRange(user.UserRoles.Where(userRole => userRole != retainedRole));
        user.UserRoles = retainedRole is null
            ? [new UserRole { UserId = user.Id, RoleId = targetRole.Id, Role = targetRole }]
            : [retainedRole];
        user.TokenVersion++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    private static AdminUserResponse ToResponse(AppUser user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.IsActive,
        user.CreatedAt,
        user.UserRoles.Select(userRole => userRole.Role.Name).Order().ToArray());
}
