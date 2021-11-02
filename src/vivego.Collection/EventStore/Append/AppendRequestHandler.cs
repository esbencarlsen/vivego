using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.EventStore;
using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.EventStore.Append
{
	public sealed class AppendRequestHandler : IRequestHandler<AppendRequest, Version>
	{
		private readonly IEventStore _eventStore;
		private readonly IKeyValueStore _keyValueStore;

		public AppendRequestHandler(
			IEventStore eventStore,
			IKeyValueStore keyValueStore)
		{
			_eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<Version> Handle(AppendRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			EventStoreState state = await _eventStore.GetState(request.StreamId, cancellationToken).ConfigureAwait(false);
			switch (request.ExpectedVersion)
			{
				case ExpectedVersion.StreamExists:
					if (state.Version < 0)
					{
						throw new WrongExpectedVersionException(
							$"Stream does not exist: Append: {request.StreamId}", state.Version,
							request.ExpectedVersion);
					}

					break;
				case ExpectedVersion.Any:
					break;
				//case ExpectedVersion.NoStream:
				default:
					if (request.ExpectedVersion != state.Version)
					{
						throw new WrongExpectedVersionException($"Append: {request.StreamId}", state.Version, request.ExpectedVersion);
					}

					break;
			}

			Version version = new()
			{
				Begin = state.Version + 1
			};
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			foreach (EventData eventData in request.EventDatas)
			{
				string key = RequestHandlerHelper.MakeKey(request.StreamId, ++state.Version);
				RecordedEvent recordedEvent = new()
				{
					CreatedAt = now,
					EventNumber = state.Version,
					Id = request.StreamId,
					Type = eventData.Type
				};
				recordedEvent.Data.Add(eventData.Data);

				SetKeyValueEntry setKeyValueEntry = new()
				{
					Key = key,
					ETag = string.Empty,
					Value = recordedEvent.ToNullableBytes(),
					ExpiresInSeconds = eventData.GetEventDataKeyValueTtlSeconds() ?? 0
				};
				await _keyValueStore
					.Set(setKeyValueEntry, cancellationToken)
					.ConfigureAwait(false);
				state.Count++;
				state.ExpiresInSeconds = eventData.GetStateDataKeyValueTtlSeconds() ?? 0;
			}

			await _eventStore.SetState(request.StreamId, state, cancellationToken).ConfigureAwait(false);
			version.End = state.Version;
			return version;
		}
	}
}
