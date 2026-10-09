using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildFlow.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Tests.Infrastructure;

internal static class UnitTestSupport
{
    // A provider is configured for model metadata only. Unit tests never open this connection.
    public static BuildFlowDbContext MainDb() => new(new DbContextOptionsBuilder<BuildFlowDbContext>()
        .UseNpgsql("Host=localhost;Database=unused_unit_test;Username=unused;Password=unused").Options);
    public static AppDbContext ProcurementDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=unused_unit_test;Username=unused;Password=unused").Options);
    public static IReadOnlyList<ValidationResult> Validate(object dto)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true);
        return errors;
    }
    public static void SetActor(ControllerBase controller, Guid actor, string role = "ProjectManager") =>
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, actor.ToString()), new Claim(ClaimTypes.Role, role)
            ], "UnitTest")) } };
}
