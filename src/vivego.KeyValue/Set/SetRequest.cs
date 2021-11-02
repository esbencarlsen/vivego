using MediatR;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue.Set;

public readonly record struct SetRequest(SetKeyValueEntry Entry) : IRequest<string>;
