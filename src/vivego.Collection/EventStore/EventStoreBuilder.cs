using System.Collections.Generic;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.EventStore.Append;
using vivego.Collection.EventStore.Delete;
using vivego.Collection.EventStore.GetAll;
using vivego.Collection.EventStore.GetOptions;
using vivego.Collection.EventStore.GetReverse;
using vivego.Collection.EventStore.GetState;
using vivego.Collection.EventStore.SetOptions;
using vivego.Collection.EventStore.SetState;
using vivego.EventStore;
using vivego.MediatR;
using vivego.ServiceBuilder;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public sealed class EventStoreBuilder : DefaultServiceBuilder<IEventStore>
	{
		public EventStoreBuilder(
			string name,
			IServiceCollection serviceCollection) : base(name, serviceCollection)
		{
			Services.AddSingleton<IEventStore>(sp => ActivatorUtilities.CreateInstance<DefaultEventStore>(sp, name));

			DependsOn<IMemoryCache>();
			Services.AddSingleton<IRequestHandler<GetStateRequest, EventStoreState>, GetStateRequestHandler>();
			Services.AddSingleton<IRequestHandler<SetStateRequest, Unit>, SetStateRequestHandler>();
			Services.AddSingleton<IRequestHandler<AppendRequest, Version>, AppendRequestHandler>();
			Services.AddSingleton<IRequestHandler<DeleteRequest, Unit>, DeleteRequestHandler>();
			Services.AddSingleton<IRequestHandler<SetOptionsRequest, Unit>, SetOptionsRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetOptionsRequest, EventStreamOptions>, GetOptionsRequestHandler>();
			Services.AddSingleton<GetRequestHandler>();
			Services.AddSingleton<IRequestHandler<GetRequest, IAsyncEnumerable<RecordedEvent>>>(sp => sp.GetRequiredService<GetRequestHandler>());
			Services.AddSingleton<IPipelineBehavior<GetRequest, IAsyncEnumerable<RecordedEvent>>>(sp => sp.GetRequiredService<GetRequestHandler>());

			Services.AddSingleton<IRequestHandler<GetReverseRequest, IAsyncEnumerable<RecordedEvent>>, GetReverseRequestHandler>();

			// Exception Handlers
			Services.AddExceptionLoggingPipelineBehaviour<AppendRequest>(request => $"Error while appending event to stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<DeleteRequest>(request => $"Error while deleting event store stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<GetRequest>(request => $"Error while retrieving all data from event store stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<GetOptionsRequest>(request => $"Error while retrieving options from event store stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<GetReverseRequest>(request => $"Error while retrieving all data in reverse from event store stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<GetStateRequest>(request => $"Error while retrieving state for stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<SetOptionsRequest>(request => $"Error while setting stream options for stream stream: {request.StreamId}");
			Services.AddExceptionLoggingPipelineBehaviour<SetStateRequest>(request => $"Error while setting state for stream stream: {request.StreamId}");
		}
	}
}
