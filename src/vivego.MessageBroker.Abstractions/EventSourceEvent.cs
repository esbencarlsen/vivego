using System;
using System.Collections.Generic;

namespace vivego.MessageBroker.Abstractions;

[Serializable]
public record EventSourceEvent(long EventId, DateTimeOffset CreatedAt, byte[] Data, IDictionary<string, string>? MetaData);
