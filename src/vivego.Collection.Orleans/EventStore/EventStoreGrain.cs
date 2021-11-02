using System;
using System.Threading;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;

using vivego.Collection.EventStore;
using vivego.EventStore;
using vivego.ServiceBuilder.Abstractions;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.Orleans.EventStore
{
	public sealed class EventStoreGrain : Grain, IEventStoreGrain
	{
		private readonly IServiceManager<IEventStore> _eventStoreManager;

		public EventStoreGrain(IServiceManager<IEventStore> eventStoreManager)
		{
			_eventStoreManager = eventStoreManager ?? throw new ArgumentNullException(nameof(eventStoreManager));
		}

		public async Task<Version> Append(string eventStoreName, long expectedVersion, params EventData[] eventDatas)
		{
			RequestContext.Set(nameof(EventStoreGrain), true);
			try
			{
				return await _eventStoreManager
					.Get(eventStoreName)
					.Append(this.GetPrimaryKeyString(), expectedVersion, eventDatas, CancellationToken.None)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(EventStoreGrain));
			}
		}

		public async Task Delete(string eventStoreName)
		{
			RequestContext.Set(nameof(EventStoreGrain), true);
			try
			{
				await _eventStoreManager.Get(eventStoreName)
					.Delete(this.GetPrimaryKeyString(), CancellationToken.None)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(EventStoreGrain));
			}
		}

		public async Task SetState(string eventStoreName, EventStoreState eventStoreState)
		{
			RequestContext.Set(nameof(EventStoreGrain), true);
			try
			{
				await _eventStoreManager.Get(eventStoreName)
					.SetState(this.GetPrimaryKeyString(), eventStoreState, CancellationToken.None)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(EventStoreGrain));
			}
		}
	}
}