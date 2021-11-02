using System;
using System.Data;
using System.Threading.Tasks;

using MediatR;

using Orleans;
using Orleans.Runtime;

using vivego.Scheduler.Model;
using vivego.Serializer;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Scheduler.Orleans
{
	public sealed class SchedulerGrain : Grain, ISchedulerGrain
	{
		private readonly IServiceManager<IScheduler> _schedulerServiceManager;
		private readonly IServiceManager<ISerializer> _serializerServiceManager;

		public SchedulerGrain(
			IServiceManager<IScheduler> schedulerServiceManager,
			IServiceManager<ISerializer> serializerServiceManager)
		{
			_schedulerServiceManager = schedulerServiceManager ?? throw new ArgumentNullException(nameof(schedulerServiceManager));
			_serializerServiceManager = serializerServiceManager ?? throw new ArgumentNullException(nameof(serializerServiceManager));
		}

		public async Task Schedule(ScheduledRequest request)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			RequestContext.Set(nameof(SchedulerGrain), true);
			try
			{
				INotification? notification = await _serializerServiceManager
					.Get(this.GetPrimaryKeyString())
					.Deserialize<INotification>(request.Notification)
					.ConfigureAwait(true);
				if (notification is null)
				{
					throw new NoNullAllowedException("Notification cannot be null");
				}
				
				await _schedulerServiceManager
					.Get(this.GetPrimaryKeyString())
					.Schedule(request.Id,
						notification,
						TimeSpan.FromMilliseconds(request.TriggerInMilliSeconds),
						request.RepeatEveryMilliSeconds > 0 ? TimeSpan.FromMilliseconds(request.RepeatEveryMilliSeconds): default)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(SchedulerGrain));
			}
		}

		public async Task Cancel(string id)
		{
			RequestContext.Set(nameof(SchedulerGrain), true);
			try
			{
				await _schedulerServiceManager
					.Get(this.GetPrimaryKeyString())
					.Cancel(id)
					.ConfigureAwait(true);
			}
			finally
			{
				RequestContext.Remove(nameof(SchedulerGrain));
			}
		}
	}
}