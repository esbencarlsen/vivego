using System;
using System.Collections.Generic;

using MediatR;

namespace vivego.MessageBroker.EventStore.Append;

#pragma warning disable CA1801 // Without this compiler generates false positive
public readonly record struct AppendRequest(
	string Topic,
	byte[] Data,
	TimeSpan? TimeToLive = default,
	IDictionary<string, string>? MetaData = default) : IRequest<long>;
