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

    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    public Task<AdminUserResponse> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        CancellationToken cancellationToken) =>
        adminService.AssignRoleAsync(userId, request.Role, cancellationToken);
}
