using MediatR;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue.Get;

public readonly record struct GetRequest(string Key) : IRequest<KeyValueEntry>;
