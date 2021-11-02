using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.logger.web;

namespace OrleansWeb.Silo
{
	public sealed class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services
				.AddMemoryCache()
				.AddLoggingMiddleware()
				.AddLoggingHandler();
		}

		public void Configure(IApplicationBuilder app,
			IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app
				.UseLoggingMiddleware()
				.UseRouting()
				.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();
				});
		}
	}
}
