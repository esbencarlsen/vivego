using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace vivego.ServiceInvocation.Tests
{
	public sealed class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
	{
		protected override IHostBuilder CreateHostBuilder()
		{
			return new HostBuilder().ConfigureWebHostDefaults(_ => { });
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);

			builder
				.UseSolutionRelativeContentRoot("..\\..\\")
				.UseStartup<TStartup>();
		}
	}
}
