using System.Collections.Generic;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Queue.Append;
using vivego.Collection.Queue.Get;
using vivego.Collection.Queue.GetAll;
using vivego.Collection.Queue.GetAllReverse;
using vivego.Collection.Queue.GetState;
using vivego.Collection.Queue.PeekFirst;
using vivego.Collection.Queue.PeekLast;
using vivego.Collection.Queue.Prepend;
using vivego.Collection.Queue.SetState;
using vivego.Collection.Queue.Truncate;
using vivego.Collection.Queue.TryTakeFirst;
using vivego.Collection.Queue.TryTakeLast;
using vivego.MediatR;
using vivego.Queue.Model;
using vivego.ServiceBuilder;

namespace vivego.Collection.Queue
{
	public sealed class QueueBuilder : DefaultServiceBuilder<IQueue>
	{
		public QueueBuilder(string name, IServiceCollection serviceCollection) : base(name, serviceCollection)
		{
			Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<DefaultQueue>(sp, name));
			Services.AddSingleton<IQueue>(sp => sp.GetRequiredService<DefaultQueue>());
			Services.AddSingleton<IQueueState>(sp => sp.GetRequiredService<DefaultQueue>());
			Services.AddSingleton<IRequestHandler<AppendRequest, long?>, AppendRequestHandler>();
			Services.AddSingleton<IRequestHandler<TruncateRequest, Unit>, TruncateRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetRequest, IQueueEntry?>, GetRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetAllRequest, IAsyncEnumerable<IQueueEntry>>, GetAllRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetAllReverseRequest, IAsyncEnumerable<IQueueEntry>>, GetAllReverseRequestHandler>();
			Services.AddSingleton<IRequestHandler<PeekFirstRequest, IQueueEntry?>, PeekFirstRequestHandler>();
			Services.AddSingleton<IRequestHandler<PeekLastRequest, IQueueEntry?>, PeekLastRequestHandler>();
			Services.AddSingleton<IRequestHandler<PrependRequest, long?>, PrependRequestHandler>();
			Services.AddSingleton<IRequestHandler<TryTakeFirstRequest, IQueueEntry?>, TryTakeFirstRequestHandler>();
			Services.AddSingleton<IRequestHandler<TryTakeLastRequest, IQueueEntry?>, TryTakeLastRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetStateRequest, QueueState>, GetStateRequestHandler>();
			Services.AddSingleton<IRequestHandler<SetStateRequest, Unit>, SetStateRequestHandler>();

			// Single Thread Pipeline behaviour for writes
			this.AddSingleThreadedPipelineBehaviour<AppendRequest, long?>(request => request.Id);
			this.AddSingleThreadedPipelineBehaviour<TruncateRequest, Unit>(request => request.Id);
			this.AddSingleThreadedPipelineBehaviour<PrependRequest, long?>(request => request.Id);
			this.AddSingleThreadedPipelineBehaviour<TryTakeFirstRequest, IQueueEntry?>(request => request.Id);
			this.AddSingleThreadedPipelineBehaviour<TryTakeLastRequest, IQueueEntry?>(request => request.Id);

			// Exception Handlers
			this.AddExceptionLoggingPipelineBehaviour<AppendRequest>(_ => $"Error while appending to queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<TruncateRequest>(_ => $"Error while clearing queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<GetRequest>(_ => $"Error while get entry from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<GetAllRequest>(_ => $"Error while getting all from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<GetAllReverseRequest>(_ => $"Error while getting all reverse from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<PeekFirstRequest>(_ => $"Error while calling peek first from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<PeekLastRequest>(_ => $"Error while calling peek last from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<PrependRequest>(_ => $"Error while prepending from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<TryTakeFirstRequest>(_ => $"Error while trying to take first from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<TryTakeLastRequest>(_ => $"Error while trying to take last from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<GetStateRequest>(_ => $"Error while getting state from queue: {Name}");
			this.AddExceptionLoggingPipelineBehaviour<SetStateRequest>(_ => $"Error while setting state from queue: {Name}");
		}
	}
}
