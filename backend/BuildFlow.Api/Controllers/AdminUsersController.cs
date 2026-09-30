using BuildFlow.Api.DTOs;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildFlow.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = SystemRoles.Administrator)]
public sealed class AdminUsersController(IAdminService adminService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<AdminUserResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyCollection<AdminUserResponse>> GetUsers(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        adminService.GetUsersAsync(search, cancellationToken);

    [HttpGet("{userId:guid}")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    public Task<AdminUserResponse> GetUser(Guid userId, CancellationToken cancellationToken) =>
        adminService.GetUserAsync(userId, cancellationToken);

    [HttpPost]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await adminService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, user);
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    public Task<AdminUserResponse> UpdateUser(
        Guid userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken) =>
        adminService.UpdateUserAsync(userId, request, cancellationToken);

    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    public Task<AdminUserResponse> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        CancellationToken cancellationToken) =>
        adminService.AssignRoleAsync(userId, request.Role, cancellationToken);

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await adminService.DeleteUserAsync(userId, cancellationToken);
        return NoContent();
    }
}
