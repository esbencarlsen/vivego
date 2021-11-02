using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.Scheduler
{
	public interface IScheduler : INamedService
	{
		Task Schedule(string id,
			INotification notification,
			TimeSpan triggerIn,
			TimeSpan? repeatEvery = default,
			CancellationToken cancellationToken = default);
		Task Cancel(string id, CancellationToken cancellationToken = default);

		Task<IScheduledNotification?> Get(string id, CancellationToken cancellationToken = default);
		IAsyncEnumerable<IScheduledNotification> GetAll(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
	}
}
