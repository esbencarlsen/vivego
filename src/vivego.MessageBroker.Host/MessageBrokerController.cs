using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.Host;

[ApiController]
public sealed class MessageBrokerController : ControllerBase
{
	private readonly IMessageBroker _messageBroker;

	public MessageBrokerController(IMessageBroker messageBroker)
	{
		_messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
	}

	[HttpPost("/{topic:required:minlength(1):maxlength(512)}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Publish(
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string topic,
		[FromQuery]
		[ModelBinder(BinderType = typeof(TimeSpanBinder))]
		TimeSpan? timeToLive = default)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));

		if (!Request.Body.CanRead)
		{
			return BadRequest("No content");
		}

		await using MemoryStream memoryStream = new();
		await Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);

		IDictionary<string, string> metaData = new Dictionary<string, string>
		{
			{ HeaderNames.ContentType, Request.Headers.ContentType }
		};

		await _messageBroker
			.Publish(topic, memoryStream.ToArray(), timeToLive, metaData, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return Ok();
	}

	[HttpGet("/{subscriptionId:required:minlength(1):maxlength(512)}/{eventId:long}")]
	[Produces("application/json")]
	[Consumes("application/json")]
	[ProducesResponseType(typeof(MessageBrokerEvent), StatusCodes.Status200OK, "application/json")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetEvent(
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string subscriptionId,
		long eventId)
	{
		MessageBrokerEvent? messageBrokerEvent = await _messageBroker
			.GetEvent(subscriptionId, eventId, HttpContext.RequestAborted)
			.ConfigureAwait(false);
		if (messageBrokerEvent is null)
		{
			return NotFound();
		}

		return Ok(messageBrokerEvent);
	}

	[HttpGet("/{subscriptionId:required:minlength(1):maxlength(512)}")]
	[Produces("json/event-stream")]
	[Consumes("application/json")]
	public async Task Get(
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string subscriptionId,
		[FromQuery] long? fromId = default,
		[FromQuery] bool stream = false,
		[FromQuery] bool reverse = false)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (stream && reverse) throw new ArgumentException("Cannot both stream and get in reverse.", nameof(reverse));

		IAsyncEnumerable<byte[]> bytesAsyncEnumerable = _messageBroker
			.MakeSerializedGetStream(subscriptionId, fromId, stream, reverse, HttpContext.RequestAborted);
		if (HttpContext.WebSockets.IsWebSocketRequest)
		{
			using WebSocket webSocket = await HttpContext.WebSockets
				.AcceptWebSocketAsync()
				.ConfigureAwait(false);

			Task webSocketReadAndDiscardTask = WebSocketReadAndDiscard(webSocket, HttpContext.RequestAborted);
			await foreach (byte[] bytes in bytesAsyncEnumerable.ConfigureAwait(false))
			{
				await webSocket
					.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, HttpContext.RequestAborted)
					.ConfigureAwait(false);
			}

			if (webSocket.State == WebSocketState.Open)
			{
				await webSocket
					.CloseAsync(WebSocketCloseStatus.NormalClosure, default, HttpContext.RequestAborted)
					.ConfigureAwait(false);
			}

			await webSocketReadAndDiscardTask.ConfigureAwait(false);
		}
		else
		{
			Response.ContentType = "text/event-stream";
			await foreach (byte[] bytes in bytesAsyncEnumerable.ConfigureAwait(false))
			{
				await Response.Body
					.WriteAsync(bytes, HttpContext.RequestAborted)
					.ConfigureAwait(false);
			}
		}
	}

	private async Task WebSocketReadAndDiscard(WebSocket socket, CancellationToken cancellationToken)
	{
		using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

		ValueWebSocketReceiveResult result = await socket
			.ReceiveAsync(memoryOwner.Memory, cancellationToken)
			.ConfigureAwait(false);
		while (!result.EndOfMessage)
		{
			result = await socket
				.ReceiveAsync(memoryOwner.Memory, cancellationToken)
				.ConfigureAwait(false);
		}

		if (socket.State == WebSocketState.CloseReceived)
		{
			await socket
				.CloseAsync(WebSocketCloseStatus.NormalClosure, default, HttpContext.RequestAborted)
				.ConfigureAwait(false);
		}
	}

	[HttpPost("/Subscribe/{subscriptionId:required:minlength(1):maxlength(512)}/{type}/{pattern:required:minlength(1):maxlength(512)}")]
	[Produces("application/json")]
	[Consumes("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> Subscribe(
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string subscriptionId,
		[FromRoute] SubscriptionType type,
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string pattern)
	{
		await _messageBroker
			.Subscribe(subscriptionId, type, pattern, HttpContext.RequestAborted)
			.ConfigureAwait(false);
		return Ok();
	}

	[HttpPost("/UnSubscribe/{subscriptionId:required:minlength(1):maxlength(512)}/{type}/{pattern:required:minlength(1):maxlength(512)}")]
	[Produces("application/json")]
	[Consumes("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> UnSubscribe(
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string subscriptionId,
		[FromRoute] SubscriptionType type,
		[FromRoute, StringLength(512, MinimumLength = 1)]
		string pattern)
	{
		await _messageBroker
			.UnSubscribe(subscriptionId, type, pattern, HttpContext.RequestAborted)
			.ConfigureAwait(false);
		return Ok();
	}
}
