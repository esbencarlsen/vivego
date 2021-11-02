using MediatR;

namespace vivego.MessageBroker.EventStore.GetNextEventId;

public readonly record struct GetNextEventIdRequest(string Topic) : IRequest<long>;
