using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using vivego.core;
using vivego.EventStore;
using vivego.Serializer;
using vivego.Serializer.Model;

using Range = vivego.EventStore.Range;
using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore
{
	public sealed class DefaultTypedEventStore<T> : IEventStore<T> where T : notnull
	{
		private readonly IEventStore _eventStore;
		private readonly ISerializer _serializer;
		private readonly TypeNameHelper _typeNameHelper = new();

		public DefaultTypedEventStore(
			IEventStore eventStore,
			ISerializer serializer)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public async Task<Version> Append(string streamId, long expectedVersion, IEnumerable<T> values, CancellationToken cancellationToken = default)
		{
			if (values is null) throw new ArgumentNullException(nameof(values));
			List<EventData> eventDatas = new();
			foreach (T value in values)
			{
				SerializedValue serializedValue = await _serializer
					.Serialize(value!, cancellationToken)
					.ConfigureAwait(false);
				EventData eventData = new()
				{
					Type = _typeNameHelper.GetTypeName<T>()
				};

				eventData.Data.Add(serializedValue.Data);
				eventDatas.Add(eventData);
			}

			return await _eventStore
				.Append(streamId, expectedVersion, eventDatas, cancellationToken)
				.ConfigureAwait(false);
		}

		public async IAsyncEnumerable<IRecordedEvent<T>> Get(string streamId, Range range, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (RecordedEvent recordedEvent in _eventStore.Get(streamId, range, cancellationToken).ConfigureAwait(false))
			{
				SerializedValue serializedValue = new();
				serializedValue.Data.Add(recordedEvent.Data);
				T? value = await _serializer
					.Deserialize<T>(serializedValue, cancellationToken)
					.ConfigureAwait(false);
				if (value is not null)
				{
					yield return new RecordedEvent<T>(recordedEvent, value);
				}
			}
		}

		public async IAsyncEnumerable<IRecordedEvent<T>> GetReverse(string streamId, Range range, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (RecordedEvent recordedEvent in _eventStore.GetReverse(streamId, range, cancellationToken).ConfigureAwait(false))
			{
				SerializedValue serializedValue = new();
				serializedValue.Data.Add(recordedEvent.Data);
				T? value = await _serializer
					.Deserialize<T>(serializedValue, cancellationToken)
					.ConfigureAwait(false);
				if (value is not null)
				{
					yield return new RecordedEvent<T>(recordedEvent, value);
				}
			}
		}

		public Task Delete(string streamId, CancellationToken cancellationToken = default) =>
			_eventStore.Delete(streamId, cancellationToken);

		public Task<EventStreamOptions> GetOptions(string streamId, CancellationToken cancellationToken) =>
			_eventStore.GetOptions(streamId, cancellationToken);

		public Task SetOptions(string streamId, EventStreamOptions eventStreamOptions, CancellationToken cancellationToken) =>
			_eventStore.SetOptions(streamId, eventStreamOptions, cancellationToken);
	}
}
