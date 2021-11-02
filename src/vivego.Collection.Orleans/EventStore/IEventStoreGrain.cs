using System.Threading.Tasks;

using Orleans;

using vivego.EventStore;

namespace vivego.Collection.Orleans.EventStore
{
	public interface IEventStoreGrain : IGrainWithStringKey
	{
		Task<Version> Append(
			string eventStoreName,
			long expectedVersion,
			params EventData[] eventDatas);
		Task Delete(string eventStoreName);
		Task SetState(string eventStoreName, EventStoreState eventStoreState);
	}
}