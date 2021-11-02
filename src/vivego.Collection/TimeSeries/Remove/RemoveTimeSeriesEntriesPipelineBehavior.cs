using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.Index;

namespace vivego.Collection.TimeSeries.Remove
{
	public sealed class RemoveTimeSeriesEntriesPipelineBehavior : IPipelineBehavior<RemoveTimeSeriesEntryRequest, bool>
	{
		private readonly IIndex _index;

		public RemoveTimeSeriesEntriesPipelineBehavior(IIndex index)
		{
			_index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public async Task<bool> Handle(
			RemoveTimeSeriesEntryRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<bool> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));
			await Remove(request.TimeSeriesId, request.Id, cancellationToken).ConfigureAwait(false);
			return await next().ConfigureAwait(false);
		}

		private Task Remove(string timeSeriesId, string id, CancellationToken cancellationToken)
		{
			return _index.Remove(timeSeriesId, id, cancellationToken);
		}
	}
}
