using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrleansWeb.Silo
{
	public static class Program
	{
		public static Task Main()
		{
			return new HostBuilder()
				.ConfigureWebHostDefaults(webHostBuilder =>
				{
					webHostBuilder
						.ConfigureLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
						.CaptureStartupErrors(true)
						.UseShutdownTimeout(TimeSpan.FromMinutes(1))
						.UseStartup<Startup>();
				})
				.RunConsoleAsync();
		}
	}
}