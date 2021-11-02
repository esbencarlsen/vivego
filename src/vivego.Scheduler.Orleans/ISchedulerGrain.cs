using System.Threading.Tasks;

using Orleans;

using vivego.Scheduler.Model;

namespace vivego.Scheduler.Orleans
{
	public interface ISchedulerGrain : IGrainWithStringKey
	{
		Task Schedule(ScheduledRequest request);
		Task Cancel(string id);
	}
}