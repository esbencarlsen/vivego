using MediatR;

using Microsoft.AspNetCore.Http;

namespace vivego.logger.web
{
	public sealed record GetLogRequestOptionsRequest : IRequest<RecordOptions>
	{
		public HttpContext HttpContext { get; }

		public GetLogRequestOptionsRequest(HttpContext httpContext)
		{
			HttpContext = httpContext;
		}
	}
}