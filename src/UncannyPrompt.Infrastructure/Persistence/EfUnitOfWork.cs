using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Application;

namespace UncannyPrompt.Infrastructure;

internal sealed class EfUnitOfWork(UncannyPromptDbContext dbContext) : IUnitOfWork
{
    private readonly Dictionary<Type, object> repositories = [];

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!repositories.TryGetValue(type, out var repository))
        {
            repository = new EfRepository<T>(dbContext);
            repositories[type] = repository;
        }

        return (IRepository<T>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
