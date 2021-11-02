using MediatR;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.EventStore.GetEvent;

public readonly record struct GetEventSourceEvent(
	string Topic,
	long EventId) : IRequest<EventSourceEvent?>;
