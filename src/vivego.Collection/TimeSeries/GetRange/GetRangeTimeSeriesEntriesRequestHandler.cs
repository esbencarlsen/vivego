using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.Index;
using vivego.core;

namespace vivego.Collection.TimeSeries.GetRange
{
	public sealed class GetRangeTimeSeriesEntriesRequestHandler : IRequestHandler<GetRangeTimeSeriesEntriesRequest, IAsyncEnumerable<ITimeSeriesEntry>>
	{
		private readonly ITimeSeries _timeSeries;
		private readonly IIndex _index;

		public GetRangeTimeSeriesEntriesRequestHandler(
			ITimeSeries timeSeries,
			IIndex index)
		{
			_timeSeries = timeSeries ?? throw new ArgumentNullException(nameof(timeSeries));
			_index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public Task<IAsyncEnumerable<ITimeSeriesEntry>> Handle(
			GetRangeTimeSeriesEntriesRequest request,
			CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			return Task.FromResult(Get(request, cancellationToken));
		}

		private Task Remove(string timeSeriesId, string id, CancellationToken cancellationToken)
		{
			return _index.Remove(timeSeriesId, id, cancellationToken);
		}

		private async IAsyncEnumerable<ITimeSeriesEntry> Get(
			GetRangeTimeSeriesEntriesRequest request,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			ImmutableSortedSet<IIndexEntry> set = await _index
				.Get(request.TimeSeriesId, cancellationToken)
				.ConfigureAwait(false);
			foreach (IIndexEntry indexEntry in set)
			{
				DateTimeOffset offSet = indexEntry.Data?.AsDateTimeOffset ?? DateTimeOffset.UtcNow;
				if (!offSet.Between(request.From, request.To, true))
				{
					continue;
				}

				string id = indexEntry.Field.AsString;
				ITimeSeriesEntry? timeSeriesEntry = await _timeSeries
					.Get(request.TimeSeriesId, id, cancellationToken)
					.ConfigureAwait(false);
				if (timeSeriesEntry is null)
				{
					await Remove(request.TimeSeriesId, id, cancellationToken).ConfigureAwait(false);
					continue;
				}

				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				yield return timeSeriesEntry;
			}
		}
	}
}
