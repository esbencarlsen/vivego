using System;
using System.Threading;
using System.Threading.Tasks;

using vivego.Queue.Model;

namespace vivego.Collection.Queue
{
	public interface IQueueState
	{
		ValueTask<QueueState> GetState(string id, CancellationToken cancellationToken = default);
		ValueTask SetState(string id, QueueState queueState, TimeSpan? expiresIn, CancellationToken cancellationToken = default);
	}
}
