using System;
using System.Net.Http.Headers;

namespace vivego.ServiceInvocation
{
	public sealed record HttpInvocation
	{
		public string Method { get; }
		public Uri[] Urls { get; } = Array.Empty<Uri>();
		public HttpHeaders? Headers { get; set; }
		public byte[]? Payload { get; init; }
		public TimeSpan? RequestTimeout { get; init; }
		public TimeSpan? ResponseTimeout { get; init; }
		public bool VerifyHttpsServerCertificate { get; } = true;

		public HttpInvocation(string method, params Uri[] urls)
		{
			Method = method;
			Urls = urls;
		}
	}
	
		// public sealed class HttpInvocationValidator : AbstractValidator<HttpInvocation>
  //   	{
  //   		public HttpInvocationValidator()
  //   		{
  //   			RuleFor(invocation => invocation.Method).NotEmpty().WithMessage("Method required");
  //   			RuleFor(invocation => invocation.RequestTimeout)
  //   				.InclusiveBetween(TimeSpan.Zero, TimeSpan.FromMinutes(10))
  //   				.When(invocation => invocation.RequestTimeout.HasValue);
  //   			RuleFor(invocation => invocation.ResponseTimeout)
  //   				.InclusiveBetween(TimeSpan.Zero, TimeSpan.FromMinutes(10))
  //   				.When(invocation => invocation.RequestTimeout.HasValue);
  //   			RuleFor(invocation => invocation.Urls)
  //   				.Must(list => list.Length > 0)
  //   				.WithMessage($"At least one '{Constants.TargetDomainHeaderKey}' header is required");
  //   		}
  //   	}

}