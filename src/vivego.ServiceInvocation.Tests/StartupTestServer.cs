using System.Net.Http;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.ServiceInvocation.HttpInvocationHandler;

namespace vivego.ServiceInvocation.Tests
{
#pragma warning disable CA1822
	public sealed class StartupTestServer
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services
				.AddMemoryCache()
				.AddServiceInvocation(
					configureHttpClient: builder =>
					{
						builder
							.ConfigurePrimaryHttpMessageHandler(sp =>
							{
								IHost host = sp.GetRequiredService<IHost>();
								TestServer testServer = host.GetTestServer();
								return testServer.CreateHandler();
							});
					})
				.Map<IRequestHandler<HttpInvocationRequest, HttpResponseMessage>>();
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseRouting();
			app.UseRouter(builder =>
			{
				builder.MapGet("/ok", async context =>
				{
					context.Response.StatusCode = StatusCodes.Status200OK;
					await context.Response.StartAsync().ConfigureAwait(false);
					await context.Response.WriteAsync("Ok").ConfigureAwait(false);
				});
				builder.MapGet("/badrequest", async context =>
				{
					context.Response.StatusCode = StatusCodes.Status400BadRequest;
					await context.Response.StartAsync().ConfigureAwait(false);
					await context.Response.WriteAsync("BadRequest").ConfigureAwait(false);
				});
			});
		}
	}
}
