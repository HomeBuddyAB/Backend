// HomeBuddy_API/Repositories/EfUnitOfWork.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HomeBuddy_API.Data; 
using HomeBuddy_API.Interfaces;

namespace HomeBuddy_API.Repositories
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;

        public EfUnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _dbContext.SaveChangesAsync(ct);
        }

        // Convenience adapter: delegate the no-token overload to the token-taking implementation.
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return ExecuteInTransactionAsync(_ => action(), ct);
        }

        // Real implementation: passes the cancellation token into the action and all DB calls.
        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                // Run the user action (it can observe ct)
                await action(ct);

                // Persist changes within the same transaction
                await _dbContext.SaveChangesAsync(ct);

                // Commit the transaction
                await tx.CommitAsync(ct);
            }
            catch
            {
                // Ensure rollback on error
                try
                {
                    await tx.RollbackAsync(ct);
                }
                catch
                {
                    // Swallow rollback exceptions to not hide the original exception
                }
                throw;
            }
        }
    }
}