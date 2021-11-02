using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.Index;

namespace vivego.Collection.TimeSeries.GetCount
{
	public sealed class GetCountTimeSeriesEntriesRequestHandler : IRequestHandler<GetCountTimeSeriesEntriesRequest, long>
	{
		private readonly IIndex _index;

		public GetCountTimeSeriesEntriesRequestHandler(IIndex index)
		{
			_index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public async Task<long> Handle(GetCountTimeSeriesEntriesRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			ImmutableSortedSet<IIndexEntry> set = await _index
				.Get(request.TimeSeriesId, cancellationToken)
				.ConfigureAwait(false);
			return set.Count;
		}
	}
}
