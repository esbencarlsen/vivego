using System;
using System.Net.Http;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceInvocation.HttpInvocationHandler;

using Xunit;

namespace vivego.ServiceInvocation.Tests
{
	public sealed class ServiceInvocationEntryRequestHandlerFakeServerTests : IClassFixture<CustomWebApplicationFactory<StartupTestServer>>
	{
		private readonly IRequestHandler<HttpInvocationRequest, HttpResponseMessage> _requestHandler;

		public ServiceInvocationEntryRequestHandlerFakeServerTests(CustomWebApplicationFactory<StartupTestServer> applicationFactory)
		{
			if (applicationFactory is null) throw new ArgumentNullException(nameof(applicationFactory));
			_requestHandler = applicationFactory.Services.GetRequiredService<IRequestHandler<HttpInvocationRequest, HttpResponseMessage>>();
		}

		[Fact]
		public async Task CanMakeRequest()
		{
			HttpInvocation invocation = new("GET", new Uri("http://localhost/ok"));
			using HttpResponseMessage response = await _requestHandler
				.Handle(new HttpInvocationRequest(invocation), default)
				.ConfigureAwait(false);
			Assert.NotNull(response);
			Assert.True(response.IsSuccessStatusCode);
			string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			Assert.Equal("Ok", responseText);
		}

		[Fact]
		public async Task CanHandleBadRequest()
		{
			HttpInvocation invocation = new("GET", new Uri("http://localhost/badrequest"));
			using HttpResponseMessage response = await _requestHandler
				.Handle(new HttpInvocationRequest(invocation), default)
				.ConfigureAwait(false);
			Assert.NotNull(response);
			Assert.False(response.IsSuccessStatusCode);
			string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			Assert.Equal("BadRequest", responseText);
		}
	}
}
