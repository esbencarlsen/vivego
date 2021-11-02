using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace vivego.KeyValue.Tests.Helpers
{
	public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

	public class GrpcTestFixture<TStartup> : IDisposable where TStartup : class
	{
		private readonly IHost _host;
		private readonly TestServer _server;

		public GrpcTestFixture() : this(null)
		{
		}

		public GrpcTestFixture(Action<IServiceCollection>? initialConfigureServices)
		{
			LoggerFactory = new LoggerFactory();
#pragma warning disable CA2000 // Dispose objects before losing scope
			LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) =>
			{
				LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
			}));
#pragma warning restore CA2000 // Dispose objects before losing scope

			IHostBuilder builder = new HostBuilder()
				.ConfigureServices(services =>
				{
					initialConfigureServices?.Invoke(services);
					services.AddSingleton<ILoggerFactory>(LoggerFactory);
				})
				.ConfigureWebHostDefaults(webHost =>
				{
					webHost
						.UseTestServer()
						.UseStartup<TStartup>();
				});
			_host = builder.Start();
			_server = _host.GetTestServer();

			// Need to set the response version to 2.0.
			// Required because of this TestServer issue - https://github.com/aspnet/AspNetCore/issues/16940
			ResponseVersionHandler responseVersionHandler = new()
			{
				InnerHandler = _server.CreateHandler()
			};

			Handler = responseVersionHandler;
		}

		public LoggerFactory LoggerFactory { get; }

		public HttpMessageHandler Handler { get; }

		protected virtual void Dispose(bool disposing)
		{
			Handler.Dispose();
			_host.Dispose();
			_server.Dispose();
		}

		public void Dispose()
		{
			// Dispose of unmanaged resources.
			Dispose(true);
			// Take yourself off the finalization queue
			// to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}

		public event LogMessage? LoggedMessage;

		public IDisposable GetTestContext() => new GrpcTestContext<TStartup>(this);

		private class ResponseVersionHandler : DelegatingHandler
		{
			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
				CancellationToken cancellationToken)
			{
				HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.Version = request.Version;

				return response;
			}
		}
	}
}
