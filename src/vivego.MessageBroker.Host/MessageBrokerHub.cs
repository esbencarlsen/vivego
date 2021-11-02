using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;

using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.Client.Http;

namespace vivego.MessageBroker.Host;

public sealed class MessageBrokerHub : Hub
{
	private readonly IMessageBroker _messageBroker;

	public MessageBrokerHub(IMessageBroker messageBroker)
	{
		_messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
	}

	public Task Publish(
		[StringLength(512, MinimumLength = 1)] string topic,
		byte[] data,
		string contentType,
		TimeSpan? timeToLive = default)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		if (data is null) throw new ArgumentNullException(nameof(data));
		if (string.IsNullOrEmpty(contentType)) throw new ArgumentException("Value cannot be null or empty.", nameof(contentType));

		IDictionary<string, string> metaData = new Dictionary<string, string>
		{
			{ HeaderNames.ContentType, contentType }
		};

		return _messageBroker.Publish(topic, data, timeToLive, metaData, Context.ConnectionAborted);
	}

	public IAsyncEnumerable<MessageBrokerEventDto> Get(
		[StringLength(512, MinimumLength = 1)] string subscriptionId,
		long? fromId = default,
		bool stream = false,
		bool reverse = false)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (stream && reverse) throw new ArgumentException("Cannot both stream and get in reverse.", nameof(reverse));

		return _messageBroker.MakeGetStream(subscriptionId, fromId, stream, reverse, Context.ConnectionAborted);
	}

	public Task Subscribe(
		[StringLength(512, MinimumLength = 1)] string subscriptionId,
		SubscriptionType type,
		[StringLength(512, MinimumLength = 1)] string pattern)
	{
		if (subscriptionId is null) throw new ArgumentNullException(nameof(subscriptionId));
		if (pattern is null) throw new ArgumentNullException(nameof(pattern));
		return _messageBroker.Subscribe(subscriptionId, type, subscriptionId, Context.ConnectionAborted);
	}

	public Task UnSubscribe(
		[StringLength(512, MinimumLength = 1)] string subscriptionId,
		SubscriptionType type,
		[StringLength(512, MinimumLength = 1)] string pattern)
	{
		if (subscriptionId is null) throw new ArgumentNullException(nameof(subscriptionId));
		if (pattern is null) throw new ArgumentNullException(nameof(pattern));
		return _messageBroker.UnSubscribe(subscriptionId, type, pattern, Context.ConnectionAborted);
	}
}
