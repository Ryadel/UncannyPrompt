using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

internal sealed class EfRepository<T>(UncannyPromptDbContext dbContext) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => dbContext.Set<T>();

    public ValueTask<T?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<T>().FindAsync([id], cancellationToken);

    public Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        dbContext.Set<T>().AddAsync(entity, cancellationToken).AsTask();

    public void Remove(T entity) => dbContext.Set<T>().Remove(entity);
}
