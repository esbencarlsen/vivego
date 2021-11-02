using System;
using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

using vivego.Collection.EventStore;
using vivego.EventStore;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Collection.Orleans.EventStore
{
	[Reentrant]
	public sealed class EventStoreStateGrain : Grain, IEventStoreStateGrain
	{
		private readonly IServiceManager<IEventStore> _eventStoreManager;

		public EventStoreStateGrain(IServiceManager<IEventStore> eventStoreManager)
		{
			_eventStoreManager = eventStoreManager ?? throw new ArgumentNullException(nameof(eventStoreManager));
		}

		public async Task<EventStoreState> GetState(string eventStoreName)
		{
			RequestContext.Set(nameof(EventStoreGrain), true);
			try
			{
				return await _eventStoreManager.Get(eventStoreName)
					.GetState(this.GetPrimaryKeyString(), CancellationToken.None)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(EventStoreGrain));
			}
		}
	}
}