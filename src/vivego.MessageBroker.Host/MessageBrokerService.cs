// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// using Grpc.Core;
//
// using vivego.MessageBroker.Abstractions;
//
// namespace vivego.MessageBroker.Host;
//
// public sealed class MessageBrokerService : vivego.MessageBroker.MessageBrokerService.MessageBrokerServiceBase
// {
// 	private readonly IMessageBroker _messageBroker;
//
// 	public MessageBrokerService(IMessageBroker messageBroker)
// 	{
// 		_messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
// 	}
//
// 	public override async Task<PublishReply> Publish(PublishRequest request, ServerCallContext context)
// 	{
// 		if (request is null) throw new ArgumentNullException(nameof(request));
// 		if (context is null) throw new ArgumentNullException(nameof(context));
// 		await _messageBroker
// 			.Publish(request.Topic, request.Data.ToByteArray(), default, default, context.CancellationToken)
// 			.ConfigureAwait(false);
// 		return new PublishReply();
// 	}
//
// 	public override async Task Get(GetRequest request, IServerStreamWriter<Abstractions.MessageBrokerEvent> responseStream, ServerCallContext context)
// 	{
// 		if (request is null) throw new ArgumentNullException(nameof(request));
// 		if (responseStream is null) throw new ArgumentNullException(nameof(responseStream));
// 		if (context is null) throw new ArgumentNullException(nameof(context));
// 		IAsyncEnumerable<Abstractions.MessageBrokerEvent> asyncEnumerable = _messageBroker
// 			.MakeRawGetStream(request.SubscriptionId, request.FromId, request.Take, request.Stream, context.CancellationToken);
// 		await foreach (Abstractions.MessageBrokerEvent messageBrokerEvent in asyncEnumerable.ConfigureAwait(false))
// 		{
// 			await responseStream
// 				.WriteAsync(messageBrokerEvent)
// 				.ConfigureAwait(false);
// 		}
// 	}
//
// 	public override async Task<SubscribeReply> Subscribe(SubscribeRequest request, ServerCallContext context)
// 	{
// 		if (request is null) throw new ArgumentNullException(nameof(request));
// 		if (context is null) throw new ArgumentNullException(nameof(context));
// 		await _messageBroker
// 			.Subscribe(request.SubscriptionId, request.Type, request.Pattern, context.CancellationToken)
// 			.ConfigureAwait(false);
// 		return new SubscribeReply();
// 	}
//
// 	public override async Task<UnSubscribeReply> UnSubscribe(UnSubscribeRequest request, ServerCallContext context)
// 	{
// 		if (request is null) throw new ArgumentNullException(nameof(request));
// 		if (context is null) throw new ArgumentNullException(nameof(context));
// 		await _messageBroker
// 			.UnSubscribe(request.SubscriptionId, request.Type, request.Pattern, context.CancellationToken)
// 			.ConfigureAwait(false);
// 		return new UnSubscribeReply();
// 	}
// }
