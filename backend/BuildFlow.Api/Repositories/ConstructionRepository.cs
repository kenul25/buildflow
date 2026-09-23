using BuildFlow.Api.Data;
using BuildFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildFlow.Api.Repositories;

public interface IConstructionRepository
{
    IQueryable<T> Query<T>() where T : ConstructionRecord;
    Task<T?> FindAsync<T>(Guid id, CancellationToken ct) where T : ConstructionRecord;
    void Add<T>(T entity) where T : ConstructionRecord;
    Task SaveAsync(CancellationToken ct);
}

public sealed class ConstructionRepository(BuildFlowDbContext db) : IConstructionRepository
{
    public IQueryable<T> Query<T>() where T : ConstructionRecord => db.Set<T>();
    public Task<T?> FindAsync<T>(Guid id, CancellationToken ct) where T : ConstructionRecord => db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);
    public void Add<T>(T entity) where T : ConstructionRecord => db.Set<T>().Add(entity);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
