using System;
using System.Net.Http;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceInvocation.HttpInvocationHandler;

using Xunit;

namespace vivego.ServiceInvocation.Tests
{
	public sealed class ServiceInvocationEntryRequestHandlerTests : IClassFixture<CustomWebApplicationFactory<Startup>>
	{
		private readonly IRequestHandler<HttpInvocationRequest, HttpResponseMessage> _requestHandler;

		public ServiceInvocationEntryRequestHandlerTests(CustomWebApplicationFactory<Startup> applicationFactory)
		{
			if (applicationFactory is null) throw new ArgumentNullException(nameof(applicationFactory));
			_requestHandler = applicationFactory.Services.GetRequiredService<IRequestHandler<HttpInvocationRequest, HttpResponseMessage>>();
		}

		[Fact]
		public async Task CanHandleTimeout()
		{
			HttpInvocation invocation = new ("GET", new Uri("http://localhost/ok"))
			{
				RequestTimeout = TimeSpan.FromSeconds(1),
				ResponseTimeout = TimeSpan.FromSeconds(1)
			};

			TaskCanceledException canceledException = await Assert
				.ThrowsAsync<TaskCanceledException>(() => _requestHandler.Handle(new HttpInvocationRequest(invocation), default))
				.ConfigureAwait(false);

			Assert.NotNull(canceledException);
		}
	}
}
