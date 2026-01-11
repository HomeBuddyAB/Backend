// HomeBuddy_API/Interfaces/IUnitOfWork.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeBuddy_API.Interfaces
{
	public interface IUnitOfWork
	{
		Task<int> SaveChangesAsync(CancellationToken ct = default);
		Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    }
}
