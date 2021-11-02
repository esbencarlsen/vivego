using MediatR;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue.Delete;

public readonly record struct DeleteRequest(DeleteKeyValueEntry Entry) : IRequest<bool>;
