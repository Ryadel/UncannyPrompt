using UncannyPrompt.Domain;

namespace UncannyPrompt.Application;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    ValueTask<T?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Remove(T entity);
}
