#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.Collection.TimeSeries
{
	public interface ITimeSeries : INamedService
	{
		Task AddOrUpdate(string timeSeriesId, string id, DateTimeOffset dateTimeOffset, Value data, CancellationToken cancellationToken = default);
		Task<bool> Remove(string timeSeriesId, string id, CancellationToken cancellationToken = default);
		Task<ITimeSeriesEntry?> Get(string timeSeriesId, string id, CancellationToken cancellationToken = default);
		IAsyncEnumerable<ITimeSeriesEntry> GetRange(string timeSeriesId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
	}
}
