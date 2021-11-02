using System.Net.Http;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceInvocation.HttpInvocationHandler;

namespace vivego.ServiceInvocation.Tests
{
#pragma warning disable CA1822
	public sealed class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddControllers();

			services
				.AddMemoryCache()
				.AddServiceInvocation()
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
					await HttpResponseWritingExtensions.WriteAsync(context.Response, "Ok").ConfigureAwait(false);
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
