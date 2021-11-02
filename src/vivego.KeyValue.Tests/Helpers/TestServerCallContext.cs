using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;

namespace vivego.KeyValue.Tests.Helpers
{
	public sealed class TestServerCallContext : ServerCallContext
	{
		private TestServerCallContext(Metadata requestHeaders, CancellationToken cancellationToken)
		{
#pragma warning disable MA0056 // Do not call overridable members in constructor
			RequestHeadersCore = requestHeaders ?? throw new ArgumentNullException(nameof(requestHeaders));
			CancellationTokenCore = cancellationToken;
			ResponseTrailersCore = new Metadata();
			AuthContextCore = new AuthContext(string.Empty, new Dictionary<string, List<AuthProperty>>(StringComparer.Ordinal));
#pragma warning restore MA0056 // Do not call overridable members in constructor
		}

		public Metadata? ResponseHeaders { get; private set; }

		protected override string MethodCore => "MethodName";
		protected override string HostCore => "HostName";
		protected override string PeerCore => "PeerName";
		protected override DateTime DeadlineCore => DateTime.UtcNow;
		protected override Metadata RequestHeadersCore { get; }
		protected override CancellationToken CancellationTokenCore { get; }
		protected override Metadata ResponseTrailersCore { get; }
		protected override Status StatusCore { get; set; }
		protected override WriteOptions? WriteOptionsCore { get; set; }
		protected override AuthContext AuthContextCore { get; }

		protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options) =>
#pragma warning disable MA0025 // TODO Implement the functionality
			throw new NotImplementedException();
#pragma warning restore MA0025 // TODO Implement the functionality

		protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
		{
			if (ResponseHeaders is not null)
				throw new InvalidOperationException("Response headers have already been written.");

			ResponseHeaders = responseHeaders;
			return Task.CompletedTask;
		}

		public static TestServerCallContext Create(Metadata? requestHeaders = null,
			CancellationToken cancellationToken = default) =>
			new(requestHeaders ?? new Metadata(), cancellationToken);
	}
}
