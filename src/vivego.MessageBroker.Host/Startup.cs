using System;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using vivego.MessageBroker.MessageBroker;

namespace vivego.MessageBroker.Host;

public sealed class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public IConfiguration Configuration { get; }

	public void ConfigureServices(IServiceCollection services)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));

		services.AddSingleton<IMediator, Mediator>();
		services.AddSingleton(p => new ServiceFactory(p.GetService!));

		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "Orleans Based Message Broker", Version = "v1" });
		});
		services.AddControllers();
		services.AddMemoryCache();
		services.AddSignalR();
		services.AddGrpc();
		services.AddGrpcReflection();

		services.TryAddMessageBroker(Configuration);
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		app.UseWebSockets(new WebSocketOptions
		{
			KeepAliveInterval = TimeSpan.FromSeconds(120),
		});
		app.UseRouting();
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
			endpoints.MapHub<MessageBrokerHub>("/mb");
			//endpoints.MapGrpcService<MessageBrokerService>();
			endpoints.MapGrpcReflectionService();
		});

		app.UseSwagger();
		app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
	}
}
