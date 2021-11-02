using System.Threading.Tasks;

using Orleans;

using vivego.EventStore;

namespace vivego.Collection.Orleans.EventStore
{
	public interface IEventStoreStateGrain : IGrainWithStringKey
	{
		Task<EventStoreState> GetState(string eventStoreName);
	}
}