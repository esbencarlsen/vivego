using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Collection.TimeSeries.Add
{
	public sealed class AddTimeSeriesEntryRequestHandler : IRequestHandler<AddTimeSeriesEntryRequest>
	{
		private readonly IKeyValueStore _keyValueStore;

		public AddTimeSeriesEntryRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<Unit> Handle(AddTimeSeriesEntryRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			string key = TimeSeriesRequestHandler.MakeKey(request.TimeSeriesId, request.Id);
			SetKeyValueEntry setKeyValueEntry = new()
			{
				Key = key,
				Value = request.Data.ToNullableBytes()
			};
			setKeyValueEntry.MetaData["DateTimeOffset"] = new Value(request.DateTimeOffset).AsByteString;

			await _keyValueStore
				.Set(setKeyValueEntry, cancellationToken)
				.ConfigureAwait(false);
			return Unit.Value;
		}
	}
}
