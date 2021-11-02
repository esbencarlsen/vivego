using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.TimeSeries.Get
{
	public sealed class GetTimeSeriesEntryRequestHandler : IRequestHandler<GetTimeSeriesEntryRequest, ITimeSeriesEntry?>
	{
		private readonly IKeyValueStore _keyValueStore;

		public GetTimeSeriesEntryRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<ITimeSeriesEntry?> Handle(
			GetTimeSeriesEntryRequest request,
			CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			string key = TimeSeriesRequestHandler.MakeKey(request.TimeSeriesId, request.Id);
			KeyValueEntry keyValueEntry = await _keyValueStore
				.Get(key, cancellationToken)
				.ConfigureAwait(false);

			if (keyValueEntry is null || keyValueEntry.Value.IsNull())
			{
				return default;
			}

			DateTimeOffset offset = keyValueEntry.MetaData.TryGetValue("DateTimeOffset", out ByteString? offsetByteString)
				? new Value(offsetByteString).AsDateTimeOffset
				: DateTimeOffset.UtcNow;

			return new DefaultTimeSeriesEntry(request.Id, keyValueEntry.Value.ToBytes()!, offset);
		}
	}
}
