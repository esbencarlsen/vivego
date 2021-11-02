using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.Index;

namespace vivego.Collection.TimeSeries.Add
{
	public sealed class AddTimeSeriesEntriesPipelineBehavior : IPipelineBehavior<AddTimeSeriesEntryRequest, Unit>
	{
		private readonly IIndex _index;

		public AddTimeSeriesEntriesPipelineBehavior(IIndex index)
		{
			_index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public async Task<Unit> Handle(
			AddTimeSeriesEntryRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<Unit> next)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (next is null) throw new ArgumentNullException(nameof(next));

			await _index
				.Add(request.TimeSeriesId, request.Id, request.DateTimeOffset, cancellationToken)
				.ConfigureAwait(false);

			return await next().ConfigureAwait(false);
		}
	}
}
