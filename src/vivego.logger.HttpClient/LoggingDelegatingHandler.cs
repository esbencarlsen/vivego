using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.logger.HttpClient
{
	public sealed class LoggingDelegatingHandler : DelegatingHandler, INamedService
	{
		private readonly IRequestResponseLogger _requestResponseLogger;

		public LoggingDelegatingHandler(string name,
			IRequestResponseLogger requestResponseLogger)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_requestResponseLogger = requestResponseLogger ?? throw new ArgumentNullException(nameof(requestResponseLogger));
		}

		public string Name { get; }

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			Stopwatch sw = Stopwatch.StartNew();

			try
			{
				HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
				sw.Stop();

				byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
				response.Content = response.Content.Copy(bytes);
				await _requestResponseLogger.Log(request, response, sw.Elapsed).ConfigureAwait(false);
				response.Content = response.Content.Copy(bytes);
				return response;
			}
			catch (Exception exception)
			{
				await _requestResponseLogger.Log(request, exception).ConfigureAwait(false);
				throw;
			}
		}
	}
}
