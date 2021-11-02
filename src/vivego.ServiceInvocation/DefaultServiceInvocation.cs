using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.ServiceInvocation.Invocation;

namespace vivego.ServiceInvocation
{
	public sealed class DefaultServiceInvocation : IServiceInvocation
	{
		private readonly IMediator _mediator;

		public DefaultServiceInvocation(
			string name,
			IMediator mediator)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public string Name { get; }

		public async Task Add(string groupId, ServiceInvocationEntry serviceInvocationEntry, CancellationToken cancellationToken = default)
		{
			if (serviceInvocationEntry is null) throw new ArgumentNullException(nameof(serviceInvocationEntry));
			if (string.IsNullOrEmpty(groupId)) throw new ArgumentException("Value cannot be null or empty.", nameof(groupId));

			ServiceInvocationRequest serviceRequest = new (groupId, serviceInvocationEntry);
			using ServiceInvocationEntryResponse response = await _mediator
				.Send(serviceRequest, cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
