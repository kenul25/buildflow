using BuildFlow.Api.Data;
using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Services;

public sealed class AdminService(
    BuildFlowDbContext dbContext,
    IPasswordService passwordService) : IAdminService
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

    public async Task<AdminUserResponse> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await UserWithRoles()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw UserNotFound();
        return ToResponse(user);
    }

    public async Task<AdminUserResponse> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
            throw EmailInUse();

        var role = await GetAssignableRoleAsync(request.Role, cancellationToken);
        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordService.Hash(request.Password),
            IsActive = request.IsActive,
            UserRoles = [new UserRole { RoleId = role.Id, Role = role }]
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<AdminUserResponse> UpdateUserAsync(
        Guid userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await UserWithRoles()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw UserNotFound();
        EnsureOperationalUser(user);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(
                item => item.Id != userId && item.NormalizedEmail == normalizedEmail,
                cancellationToken))
            throw EmailInUse();

        var role = await GetAssignableRoleAsync(request.Role, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.NormalizedEmail = normalizedEmail;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = passwordService.Hash(request.Password);
        ReplaceRole(user, role);
        user.TokenVersion++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<AdminUserResponse> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var user = await UserWithRoles()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw UserNotFound();
        EnsureOperationalUser(user);

        var targetRole = await GetAssignableRoleAsync(role, cancellationToken);

        if (user.UserRoles.Count == 1 && user.UserRoles.Single().RoleId == targetRole.Id)
            return ToResponse(user);

        ReplaceRole(user, targetRole);
        user.TokenVersion++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await UserWithRoles()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw UserNotFound();
        EnsureOperationalUser(user);
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AppUser> UserWithRoles() =>
        dbContext.Users
            .Include(item => item.UserRoles)
            .ThenInclude(userRole => userRole.Role);

    private async Task<Role> GetAssignableRoleAsync(
        string requestedRole,
        CancellationToken cancellationToken)
    {
        var roleName = SystemRoles.AssignableByAdministrator.SingleOrDefault(
            item => string.Equals(item, requestedRole.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ApiException(
                StatusCodes.Status400BadRequest,
                "invalid_role",
                "Select a valid operational role.");
        return await dbContext.Roles.SingleAsync(
            item => item.NormalizedName == roleName.ToUpperInvariant(),
            cancellationToken);
    }

    private void ReplaceRole(AppUser user, Role targetRole)
    {
        var retainedRole = user.UserRoles.SingleOrDefault(userRole => userRole.RoleId == targetRole.Id);
        dbContext.UserRoles.RemoveRange(user.UserRoles.Where(userRole => userRole != retainedRole));
        user.UserRoles = retainedRole is null
            ? [new UserRole { UserId = user.Id, RoleId = targetRole.Id, Role = targetRole }]
            : [retainedRole];
    }

    private static void EnsureOperationalUser(AppUser user)
    {
        if (user.UserRoles.Any(userRole => userRole.Role.Name == SystemRoles.Administrator))
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "administrator_account_protected",
                "Administrator accounts cannot be modified from user management.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static ApiException UserNotFound() => new(
        StatusCodes.Status404NotFound,
        "user_not_found",
        "The selected user was not found.");

    private static ApiException EmailInUse() => new(
        StatusCodes.Status409Conflict,
        "email_in_use",
        "An account already uses this email address.");

    private static AdminUserResponse ToResponse(AppUser user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.IsActive,
        user.CreatedAt,
        user.UserRoles.Select(userRole => userRole.Role.Name).Order().ToArray());
}
