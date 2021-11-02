using System;
using System.Collections.Generic;

using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using vivego.Collection.Index;
using vivego.Collection.Queue;
using vivego.Collection.TimeSeries.Add;
using vivego.Collection.TimeSeries.Get;
using vivego.Collection.TimeSeries.GetRange;
using vivego.Collection.TimeSeries.Remove;
using vivego.MediatR;
using vivego.ServiceBuilder;

namespace vivego.Collection.TimeSeries
{
	public sealed class TimeSeriesServiceBuilder : DefaultServiceBuilder<ITimeSeries>
	{
		public TimeSeriesServiceBuilder(string name,
			IServiceCollection serviceCollection,
			Func<string, IIndexCompactionStrategy> compactionStrategyFactory,
			TimeSpan? cacheTimeout = default,
			TimeSpan? timeToLive = default) : base(name, serviceCollection)
		{
			Services.AddSingleton<ITimeSeries>(sp => ActivatorUtilities.CreateInstance<DefaultTimeSeries>(sp, name));

			Services.AddSingleton<IRequestHandler<GetTimeSeriesEntryRequest, ITimeSeriesEntry?>, GetTimeSeriesEntryRequestHandler>();

			Services.AddSingleton<IIndex>(sp => new DefaultIndex(ByteArrayAscendingComparer.Instance,
				timeToLive,
				cacheTimeout.GetValueOrDefault(TimeSpan.FromMinutes(10)),
				sp.GetRequiredService<IMemoryCache>(),
				sp.GetRequiredService<IQueue>(),
				compactionStrategyFactory));

			Services.AddSingleton<IRequestHandler<AddTimeSeriesEntryRequest, Unit>, AddTimeSeriesEntryRequestHandler>();
			this.AddSingleThreadedPipelineBehaviour<AddTimeSeriesEntryRequest, Unit>(request => request.TimeSeriesId);
			Services.AddSingleton<IPipelineBehavior<AddTimeSeriesEntryRequest, Unit>, AddTimeSeriesEntriesPipelineBehavior>();

			Services.AddSingleton<IRequestHandler<RemoveTimeSeriesEntryRequest, bool>, RemoveTimeSeriesEntryRequestHandler>();
			this.AddSingleThreadedPipelineBehaviour<RemoveTimeSeriesEntryRequest, bool>(request => request.TimeSeriesId);
			Services.AddSingleton<IPipelineBehavior<RemoveTimeSeriesEntryRequest, bool>, RemoveTimeSeriesEntriesPipelineBehavior>();

			this.AddSingleThreadedPipelineBehaviour<GetRangeTimeSeriesEntriesRequest, IAsyncEnumerable<ITimeSeriesEntry>>(request => request.TimeSeriesId);
			Services.AddSingleton<IRequestHandler<GetRangeTimeSeriesEntriesRequest, IAsyncEnumerable<ITimeSeriesEntry>>, GetRangeTimeSeriesEntriesRequestHandler>();
		}
	}
}
