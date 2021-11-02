using System.Net.Http;

using vivego.core;

namespace vivego.ServiceInvocation
{
	public sealed class ServiceInvocationEntryResponse : DisposableBase
	{
		public ServiceInvocationEntryResponse(HttpResponseMessage httpResponseMessage)
		{
			HttpResponseMessage = httpResponseMessage;
		}

		public HttpResponseMessage HttpResponseMessage { get; }

		protected override void Cleanup()
		{
			HttpResponseMessage?.Dispose();
		}
	}
}
