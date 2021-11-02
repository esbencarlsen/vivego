using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue;

namespace vivego.Collection.TimeSeries.Remove
{
	public sealed class RemoveTimeSeriesEntryRequestHandler : IRequestHandler<RemoveTimeSeriesEntryRequest, bool>
	{
		private readonly IKeyValueStore _keyValueStore;

		public RemoveTimeSeriesEntryRequestHandler(IKeyValueStore keyValueStore)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
		}

		public async Task<bool> Handle(RemoveTimeSeriesEntryRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			string key = TimeSeriesRequestHandler.MakeKey(request.TimeSeriesId, request.Id);
			return await _keyValueStore.DeleteEntry(key, cancellationToken).ConfigureAwait(false);
		}
	}
}
