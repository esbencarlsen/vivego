using System;
using System.Collections.Generic;

using vivego.EventStore;

namespace vivego.Collection.EventStore
{
	public sealed class RecordedEvent<T> : IRecordedEvent<T>
	{
		private readonly RecordedEvent _recordedEvent;

		public string Id => _recordedEvent.Id;
		public long EventNumber => _recordedEvent.EventNumber;
		public DateTimeOffset CreatedAt  => DateTimeOffset.FromUnixTimeMilliseconds(_recordedEvent.CreatedAt);
		public string Type  => _recordedEvent.Type;
		public T Value { get; }

		public RecordedEvent(RecordedEvent recordedEvent, T value)
		{
			_recordedEvent = recordedEvent ?? throw new ArgumentNullException(nameof(recordedEvent));
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		private bool Equals(IRecordedEvent<T> other) => Id.Equals(other.Id, StringComparison.Ordinal)
			&& EventNumber == other.EventNumber
			&& CreatedAt.Equals(other.CreatedAt)
			&& Type.Equals(other.Type, StringComparison.Ordinal)
			&& EqualityComparer<T>.Default.Equals(Value, other.Value);

		public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is RecordedEvent<T> other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Id, EventNumber, CreatedAt, Type, Value);

		public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(EventNumber)}: {EventNumber}, {nameof(CreatedAt)}: {CreatedAt}, {nameof(Type)}: {Type}, {nameof(Value)}: {Value}";
	}
}
