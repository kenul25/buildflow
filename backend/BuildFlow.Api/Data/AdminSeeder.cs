using BuildFlow.Api.Configuration;
using BuildFlow.Api.Interfaces;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BuildFlow.Api.Data;

public sealed class AdminSeeder(
    BuildFlowDbContext dbContext,
    IPasswordService passwordService,
    IOptions<InitialAdminOptions> options,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = options.Value.Email.Trim().ToLowerInvariant();
        var normalizedEmail = email.ToUpperInvariant();
        var administratorRole = await dbContext.Roles.SingleAsync(
            role => role.NormalizedName == SystemRoles.Administrator.ToUpperInvariant(),
            cancellationToken);

        var user = await dbContext.Users
            .Include(item => item.UserRoles)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                FullName = "BuildFlow Administrator",
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordService.Hash(options.Value.Password),
                UserRoles = [new UserRole { RoleId = administratorRole.Id }]
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created the initial administrator account for {Email}.", email);
            return;
        }

        if (user.UserRoles.All(userRole => userRole.RoleId != administratorRole.Id))
        {
            user.UserRoles.Add(new UserRole { RoleId = administratorRole.Id });
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned the Administrator role to the initial administrator account for {Email}.", email);
        }
    }
}
