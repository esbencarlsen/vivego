using MediatR;

using vivego.KeyValue.Abstractions.Model;

namespace vivego.KeyValue.Set;

public sealed record SetKeyValueEntryNotification(SetKeyValueEntry SetKeyValueEntry) : INotification;
