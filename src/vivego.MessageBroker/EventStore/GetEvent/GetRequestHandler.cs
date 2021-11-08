using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Options;

using Orleans.Serialization;

using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.MessageBroker.EventStore.GetEvent;

public sealed class GetEventSourceEventHandler : IRequestHandler<GetEventSourceEvent, EventSourceEvent?>
{
	private readonly IKeyValueStore _keyValueStore;
	private readonly SerializationManager _serializationManager;

	public GetEventSourceEventHandler(
		IKeyValueStore keyValueStore,
		SerializationManager serializationManager,
		IServiceManager<IKeyValueStore> serviceManager,
		IOptions<EventStoreOptions> options)
	{
		if (serviceManager is null) throw new ArgumentNullException(nameof(serviceManager));
		if (options is null) throw new ArgumentNullException(nameof(options));
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));

		_keyValueStore = string.IsNullOrEmpty(options.Value.KeyValueStoreName)
			? serviceManager.GetAll().First()
			: serviceManager.Get(options.Value.KeyValueStoreName);
	}

	public async Task<EventSourceEvent?> Handle(GetEventSourceEvent request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);

		byte[]? bytes = await _keyValueStore
			.GetValue(DefaultEventStore.GetStoreKey(request.Topic, request.EventId), cancellationToken)
			.ConfigureAwait(false);

		EventSourceEvent? eventSourceEvent = bytes is null
			? default
			: _serializationManager.DeserializeFromByteArray<EventSourceEvent>(bytes);

		return eventSourceEvent;
	}
}
