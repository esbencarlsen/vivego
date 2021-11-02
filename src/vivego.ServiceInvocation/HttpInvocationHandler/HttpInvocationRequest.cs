using System.Net.Http;

using MediatR;

namespace vivego.ServiceInvocation.HttpInvocationHandler
{
	public sealed record HttpInvocationRequest : IRequest<HttpResponseMessage>
	{
		public HttpInvocation Invocation { get; }
		public HttpInvocationRequest(HttpInvocation invocation) => Invocation = invocation;
	}
}