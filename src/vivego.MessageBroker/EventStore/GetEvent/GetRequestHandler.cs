using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Orleans.Serialization;

using vivego.KeyValue;
using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.EventStore.GetEvent;

public sealed class GetEventSourceEventHandler : IRequestHandler<GetEventSourceEvent, EventSourceEvent?>
{
	private readonly IKeyValueStore _keyValueStore;
	private readonly SerializationManager _serializationManager;

	public GetEventSourceEventHandler(
		IKeyValueStore keyValueStore,
		SerializationManager serializationManager)
	{
		_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));
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
