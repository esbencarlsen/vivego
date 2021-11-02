using System.Threading;
using System.Threading.Tasks;

namespace vivego.Scheduler
{
	public interface ISchedulerDispatcher
	{
		Task Dispatch(IScheduledNotification scheduledNotification, CancellationToken cancellationToken);
	}
}