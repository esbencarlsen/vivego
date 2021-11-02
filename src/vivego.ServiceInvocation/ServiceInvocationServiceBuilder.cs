using System;
using System.Net;
using System.Net.Http;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

using vivego.core;
using vivego.logger.HttpClient;
using vivego.MediatR;
using vivego.ServiceBuilder;
using vivego.ServiceInvocation.HttpInvocationHandler;
using vivego.ServiceInvocation.Invocation;

namespace vivego.ServiceInvocation
{
	public sealed class ServiceInvocationServiceBuilder : DefaultServiceBuilder<IServiceInvocation>
	{
		public  ServiceInvocationServiceBuilder(string name,
			IServiceCollection serviceCollection,
			Action<DefaultServiceInvocationOptions>? configure = default,
			Action<IHttpClientBuilder>? configureHttpClient = default,
			IAsyncPolicy<ServiceInvocationEntryResponse>? policy = default) : base(name, serviceCollection)
		{
			Services
				.AddOptions<DefaultServiceInvocationOptions>()
				.Configure(options => configure?.Invoke(options))
				.PostConfigure(defaultServiceInvocationOptions => new DefaultServiceInvocationOptionsValidator().Validate(defaultServiceInvocationOptions));

			Services.AddSingleton(sp => ActivatorUtilities.CreateInstance<DefaultServiceInvocation>(sp, name));
			Services.AddSingleton<IServiceInvocation>(sp => sp.GetRequiredService<DefaultServiceInvocation>());

			this.AddExceptionLoggingPipelineBehaviour<ServiceInvocationRequest>(request => $"Error while doing ServiceInvocation for group: {request.GroupId}; Uri: {request.Entry.HttpInvocation.Urls.Join(";")}");
			this.AddLoggingPipelineBehaviour<ServiceInvocationRequest, ServiceInvocationEntryResponse>(LogLevel.Debug, (logger, request, _) => logger.LogDebug($"ServiceInvocation for group: {request.GroupId}; Uri: {request.Entry.HttpInvocation.Urls.Join(";")}"));
			this.AddSingleThreadedPipelineBehaviour<ServiceInvocationRequest, ServiceInvocationEntryResponse>(request => request.GroupId);
			this.AddRetryingPipelineBehaviour<ServiceInvocationRequest, ServiceInvocationEntryResponse>(sp =>
			{
				if (policy is null)
				{
					IOptions<DefaultServiceInvocationOptions> options = sp.GetRequiredService<IOptions<DefaultServiceInvocationOptions>>();
					AsyncRetryPolicy<ServiceInvocationEntryResponse>? retryAsync = Policy
						.Handle<HttpRequestException>()
						.OrResult<ServiceInvocationEntryResponse>(response => response.HttpResponseMessage.StatusCode >= HttpStatusCode.InternalServerError || response.HttpResponseMessage.StatusCode == HttpStatusCode.RequestTimeout)
						.WaitAndRetryAsync(options.Value.DefaultRetryDelays);
					return retryAsync;
				}

				return policy;
			});

			Services.AddSingleton<IRequestHandler<HttpInvocationRequest, HttpResponseMessage>, HttpInvocationRequestHandler>();
			//Services.AddSingleton<IRequestHandler<ServiceInvocationRequest, ServiceInvocationEntryResponse>, ServiceInvocationEntryRequestHandler>();

			DependsOn<IHttpClientFactory>();

			IHttpClientBuilder httpClientbuilder = serviceCollection.AddHttpClient(HttpInvocationRequestHandler.ServiceInvocationSecureClient);
			configureHttpClient?.Invoke(httpClientbuilder);
			if(configureHttpClient is not null)
			{
				httpClientbuilder.AddHttpProtocolRequestHandler();
			}

			IHttpClientBuilder inSecureHttpClientbuilder = serviceCollection.AddHttpClient(HttpInvocationRequestHandler.ServiceInvocationInSecureClient);
			configureHttpClient?.Invoke(inSecureHttpClientbuilder);
			if(configureHttpClient is not null)
			{
				inSecureHttpClientbuilder.AddHttpProtocolRequestHandler();
			}
		}
	}
}
