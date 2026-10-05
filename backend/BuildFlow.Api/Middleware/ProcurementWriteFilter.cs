using BuildFlow.Api.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Middleware;

// Supplier and quotation CRUD shares locks with order creation and workflow approval.
public sealed class ProcurementWriteFilter(AppDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        if (controller is not ("Suppliers" or "SupplierMaterials" or "Quotations") || HttpMethods.IsGet(context.HttpContext.Request.Method)) { await next(); return; }
        var ct = context.HttpContext.RequestAborted;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42003001)", ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(42002001)", ct);
        var executed = await next();
        if (executed.Exception == null || executed.ExceptionHandled) await tx.CommitAsync(ct);
    }
}
