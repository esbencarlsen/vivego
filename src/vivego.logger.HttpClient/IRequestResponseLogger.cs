using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace vivego.logger.HttpClient
{
	public interface IRequestResponseLogger
	{
		Task Log(
			HttpRequestMessage httpRequestMessage,
			HttpResponseMessage httpResponseMessage,
			TimeSpan requestResponseTime);
		
		Task Log(
			HttpRequestMessage httpRequestMessage,
			Exception exception);
	}
}
