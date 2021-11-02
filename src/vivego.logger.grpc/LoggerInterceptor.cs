using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace vivego.logger.grpc
{
	public sealed class LoggerInterceptor : Interceptor
	{
		public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			BlockingUnaryCallContinuation<TRequest, TResponse> continuation) =>
			base.BlockingUnaryCall(request, context, continuation);

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) =>
			base.AsyncUnaryCall(request, context, continuation);

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) =>
			base.AsyncServerStreamingCall(request, context, continuation);

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) =>
			base.AsyncClientStreamingCall(context, continuation);

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) =>
			base.AsyncDuplexStreamingCall(context, continuation);

		public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
			UnaryServerMethod<TRequest, TResponse> continuation) =>
			await base.UnaryServerHandler(request, context, continuation).ConfigureAwait(false);

		public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
			ServerCallContext context,
			ClientStreamingServerMethod<TRequest, TResponse> continuation) =>
			await base.ClientStreamingServerHandler(requestStream, context, continuation).ConfigureAwait(false);

		public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request,
			IServerStreamWriter<TResponse> responseStream,
			ServerCallContext context,
			ServerStreamingServerMethod<TRequest, TResponse> continuation)
		{
			await base.ServerStreamingServerHandler(request, responseStream, context, continuation).ConfigureAwait(false);
		}

		public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
			IServerStreamWriter<TResponse> responseStream,
			ServerCallContext context,
			DuplexStreamingServerMethod<TRequest, TResponse> continuation)
		{
			await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation).ConfigureAwait(false);
		}
	}
}
