using Microsoft.EntityFrameworkCore;
using UncannyPrompt.Application;
using UncannyPrompt.Infrastructure;

namespace UncannyPrompt.UnitTests.TestSupport;

internal sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        var options = new DbContextOptionsBuilder<UncannyPromptDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new UncannyPromptDbContext(options);
        UnitOfWork = new EfUnitOfWork(DbContext);
    }

    public UncannyPromptDbContext DbContext { get; }

    public IUnitOfWork UnitOfWork { get; }

    public Task SaveChangesAsync() => DbContext.SaveChangesAsync();

    public void Dispose() => DbContext.Dispose();
}
