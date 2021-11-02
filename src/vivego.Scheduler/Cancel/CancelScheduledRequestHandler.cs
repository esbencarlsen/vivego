using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Collection.TimeSeries;

namespace vivego.Scheduler.Cancel
{
	public sealed class CancelScheduledRequestHandler : IRequestHandler<CancelScheduledRequest>
	{
		private readonly string _name;
		private readonly ITimeSeries _timeSeries;

		public CancelScheduledRequestHandler(string name, ITimeSeries timeSeries)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_timeSeries = timeSeries ?? throw new ArgumentNullException(nameof(timeSeries));
		}

		public async Task<Unit> Handle(CancelScheduledRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (string.IsNullOrEmpty(request.Id)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Id));

			await _timeSeries.Remove(_name, request.Id, cancellationToken).ConfigureAwait(false);

			return Unit.Value;
		}
	}
}
