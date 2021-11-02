// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Net.Http.Headers;
// using System.Threading;
// using System.Threading.Tasks;
//
// using MediatR;
//
// using Microsoft.Extensions.Options;
//
// namespace vivego.ServiceInvocation.Invocation
// {
// 	public sealed class AdditionalHeadersPipelineBehaviour : IPipelineBehavior<ServiceInvocationRequest, ServiceInvocationEntryResponse>
// 	{
// 		private readonly IOptions<DefaultServiceInvocationOptions> _options;
//
// 		public AdditionalHeadersPipelineBehaviour(IOptions<DefaultServiceInvocationOptions> options)
// 		{
// 			_options = options ?? throw new ArgumentNullException(nameof(options));
// 		}
//
// 		public Task<ServiceInvocationEntryResponse> Handle(
// 			ServiceInvocationRequest request,
// 			CancellationToken cancellationToken,
// 			RequestHandlerDelegate<ServiceInvocationEntryResponse> next)
// 		{
// 			if (request is null) throw new ArgumentNullException(nameof(request));
// 			if (next is null) throw new ArgumentNullException(nameof(next));
//
// 			AddHeader(request, "queuename", request.GroupId);
// 			AddHeader(request, "maxTries", request.Entry.RetryCount.ToString(CultureInfo.InvariantCulture));
// 			if (request.Entry.RetryUntil.HasValue)
// 			{
// 				AddHeader(request, "retryUntil", request.Entry.RetryUntil.Value.ToString("O", CultureInfo.InvariantCulture));
// 			}
//
// 			return next();
// 		}
//
// 		private void AddHeader(ServiceInvocationRequest request,
// 			string key,
// 			string value)
// 		{
// 			if (request is null) throw new ArgumentNullException(nameof(request));
// 			string prefixedKey = _options.Value.HttpRequestHeaderPrefix + key;
//
// 			if (request.Entry.HttpInvocation.Headers is null)
// 			{
// 				request.Entry.HttpInvocation.Headers = new HttpContentHeaders();
// 			}
//
// 			if (!request.Entry.HttpInvocation.Headers.TryGetValues(prefixedKey, out IEnumerable<string>? values))
// 			{
// 				values = new List<string>();
// 				request.Entry.HttpInvocation.Headers.Add(prefixedKey, stringList);
// 			}
//
// 			if (!stringList.List.Contains(value))
// 			{
// 				stringList.List.Add(value);
// 			}
// 		}
// 	}
// }
