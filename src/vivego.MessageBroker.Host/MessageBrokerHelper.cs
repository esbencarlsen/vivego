using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Net.Http.Headers;

using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.Client.Http;

namespace vivego.MessageBroker.Host;

public static class MessageBrokerHelper
{
	public static IAsyncEnumerable<MessageBrokerEvent> MakeRawGetStream(
		this IMessageBroker messageBroker,
		string subscriptionId,
		long? fromId = default,
		bool stream = false,
		bool reverse = false,
		CancellationToken cancellationToken = default)
	{
		if (messageBroker is null) throw new ArgumentNullException(nameof(messageBroker));
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		if (reverse)
		{
			return stream
				? messageBroker.StreamingGet(subscriptionId, fromId, cancellationToken)
				: messageBroker.Get(subscriptionId, fromId ?? 0, cancellationToken);
		}

		return messageBroker.GetReverse(subscriptionId, fromId ?? 0, cancellationToken);
	}

	public static async IAsyncEnumerable<MessageBrokerEventDto> MakeGetStream(
		this IMessageBroker messageBroker,
		string subscriptionId,
		long? fromId = default,
		bool stream = false,
		bool reverse = false,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (messageBroker is null) throw new ArgumentNullException(nameof(messageBroker));
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		IAsyncEnumerable<MessageBrokerEventDto> asyncEnumerable = messageBroker
			.MakeRawGetStream(subscriptionId, fromId, stream, reverse, cancellationToken)
			.Select(messageBrokerEvent =>
			{
				MessageBrokerEventDto messageBrokerEventDto = new()
				{
					EventId = messageBrokerEvent.EventId,
					CreatedAt = messageBrokerEvent.CreatedAt.ToUnixTimeMilliseconds()
				};
				if (messageBrokerEvent.MetaData is not null
					&& messageBrokerEvent.MetaData.TryGetValue(HeaderNames.ContentType, out string? contentType)
					&& contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
				{
					if (messageBrokerEvent.Data.Length > 0)
					{
						messageBrokerEventDto.ExtensionData.Add("data", GetElement(messageBrokerEvent.Data));
					}
				}

				return messageBrokerEventDto;
			});

		await foreach (MessageBrokerEventDto messageBrokerEventDto in asyncEnumerable.ConfigureAwait(false))
		{
			yield return messageBrokerEventDto;
		}
	}

	public static IAsyncEnumerable<byte[]> MakeSerializedGetStream(
		this IMessageBroker messageBroker,
		string subscriptionId,
		long? fromId = default,
		bool stream = false,
		bool reverse = false,
		CancellationToken cancellationToken = default)
	{
		if (messageBroker is null) throw new ArgumentNullException(nameof(messageBroker));
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		return messageBroker
			.MakeGetStream(subscriptionId, fromId, stream, reverse, cancellationToken)
			.Select(messageBrokerEventDto =>
			{
				string serialized = JsonSerializer.Serialize(messageBrokerEventDto) + Environment.NewLine;
				byte[] bytes = Encoding.UTF8.GetBytes(serialized);
				return bytes;
			});
	}

	private static JsonElement GetElement(ReadOnlySpan<byte> span)
	{
		Utf8JsonReader utf8JsonReader = new(span);
		return JsonElement.ParseValue(ref utf8JsonReader);
	}
}
