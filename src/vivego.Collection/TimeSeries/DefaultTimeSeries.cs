using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.TimeSeries.Add;
using vivego.Collection.TimeSeries.Get;
using vivego.Collection.TimeSeries.GetRange;
using vivego.Collection.TimeSeries.Remove;

namespace vivego.Collection.TimeSeries
{
	public sealed class DefaultTimeSeries : ITimeSeries
	{
		private readonly IMediator _mediator;

		public DefaultTimeSeries(string name, IMediator mediator)
		{
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public string Name { get; }

		public Task AddOrUpdate(string timeSeriesId, string id, DateTimeOffset dateTimeOffset, Value data, CancellationToken cancellationToken = default)
		{
			if (data is null) throw new ArgumentNullException(nameof(data));
			return _mediator.Send(new AddTimeSeriesEntryRequest(timeSeriesId, dateTimeOffset, id, data.Data), cancellationToken);
		}

		public Task<bool> Remove(string timeSeriesId, string id, CancellationToken cancellationToken = default) =>
			_mediator.Send(new RemoveTimeSeriesEntryRequest(timeSeriesId, id), cancellationToken);

		public Task<ITimeSeriesEntry?> Get(string timeSeriesId, string id, CancellationToken cancellationToken = default) =>
			_mediator.Send(new GetTimeSeriesEntryRequest(timeSeriesId, id), cancellationToken);

		public async IAsyncEnumerable<ITimeSeriesEntry> GetRange(
			string timeSeriesId,
			DateTimeOffset from,
			DateTimeOffset to,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			IAsyncEnumerable<ITimeSeriesEntry> asyncEnumerable = await _mediator
				.Send(new GetRangeTimeSeriesEntriesRequest(timeSeriesId, from, to), cancellationToken)
				.ConfigureAwait(false);
			await foreach (ITimeSeriesEntry data in asyncEnumerable.ConfigureAwait(false))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				yield return data;
			}
		}
	}
}
