using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.Client.Http;

public sealed class HttpMessageBroker : IMessageBroker
{
	private readonly IHttpClientFactory _clientFactory;
	private readonly JsonSerializerOptions _defaultSerializerOptions = new(JsonSerializerDefaults.Web);

	public HttpMessageBroker(IHttpClientFactory clientFactory)
	{
		_clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
	}

	public async Task Publish(string subscriptionId,
		byte[] data,
		TimeSpan? timeToLive = default,
		IDictionary<string, string>? metaData = default,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		using HttpClient client = _clientFactory.CreateClient(nameof(HttpMessageBroker));
		using HttpContent httpContent = new ByteArrayContent(data)
		{
			Headers =
			{
				ContentType = new MediaTypeHeaderValue("application/json")
			}
		};
		using HttpResponseMessage response = await client
			.PostAsync(MakeUri(subscriptionId), httpContent, cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
	}

	public async Task<MessageBrokerEvent?> GetEvent(string subscriptionId, long eventId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (eventId < 0) throw new ArgumentOutOfRangeException(nameof(eventId));

		using HttpClient client = _clientFactory.CreateClient(nameof(HttpMessageBroker));
		using HttpRequestMessage httpRequestMessage = new()
		{
			Method = HttpMethod.Get,
			RequestUri = MakeUri(subscriptionId, eventId.ToString(CultureInfo.InvariantCulture)),
			Content = new ByteArrayContent(Array.Empty<byte>())
			{
				Headers =
				{
					ContentType = new MediaTypeHeaderValue("application/json")
				}
			}
		};
		using HttpResponseMessage httpResponseMessage = await client
			.SendAsync(httpRequestMessage, cancellationToken)
			.ConfigureAwait(false);

		if (httpResponseMessage.IsSuccessStatusCode)
		{
			return await httpResponseMessage.Content
				.ReadFromJsonAsync<MessageBrokerEvent>(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}

		return default;
	}

	public IAsyncEnumerable<MessageBrokerEvent> Get(
		string subscriptionId,
		long fromId,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		return StreamingGet(MakeUri(subscriptionId), cancellationToken);
	}

	public IAsyncEnumerable<MessageBrokerEvent> GetReverse(string subscriptionId, long fromId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		Dictionary<string, string> queryParameters = new()
		{
			{ "reverse", "true" },
			{ "fromId", fromId.ToString(CultureInfo.InvariantCulture) }
		};

		return StreamingGet(MakeUri(subscriptionId, queryParameters), cancellationToken);
	}

	public IAsyncEnumerable<MessageBrokerEvent> StreamingGet(string subscriptionId, long? fromId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));

		Dictionary<string, string> queryParameters = new()
		{
			{ "stream", "true" }
		};
		if (fromId.HasValue)
		{
			queryParameters.Add("fromId", fromId.Value.ToString(CultureInfo.InvariantCulture));
		}

		return StreamingGet(MakeUri(subscriptionId, queryParameters), cancellationToken);
	}

	public async Task Subscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));

		using HttpClient client = _clientFactory.CreateClient(nameof(HttpMessageBroker));
		using HttpRequestMessage httpRequestMessage = new()
		{
			Method = HttpMethod.Post,
			RequestUri = MakeUri("Subscribe", subscriptionId, type.ToString(), pattern),
			Content = new ByteArrayContent(Array.Empty<byte>())
			{
				Headers =
				{
					ContentType = new MediaTypeHeaderValue("application/json")
				}
			}
		};
		using HttpResponseMessage httpResponseMessage = await client
			.SendAsync(httpRequestMessage, cancellationToken)
			.ConfigureAwait(false);
		httpResponseMessage.EnsureSuccessStatusCode();
	}

	public async Task UnSubscribe(string subscriptionId, SubscriptionType type, string pattern, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));

		using HttpClient client = _clientFactory.CreateClient(nameof(HttpMessageBroker));
		using HttpRequestMessage httpRequestMessage = new()
		{
			Method = HttpMethod.Post,
			RequestUri = MakeUri("UnSubscribe", subscriptionId, type.ToString(), pattern),
			Content = new ByteArrayContent(Array.Empty<byte>())
			{
				Headers =
				{
					ContentType = new MediaTypeHeaderValue("application/json")
				}
			}
		};
		using HttpResponseMessage httpResponseMessage = await client
			.SendAsync(httpRequestMessage, cancellationToken)
			.ConfigureAwait(false);
		httpResponseMessage.EnsureSuccessStatusCode();
	}

	private static Uri MakeUri(params string[] pathSegments)
	{
		string encodedUri = string.Join("/", pathSegments.Select(HttpUtility.UrlEncode));
		return new Uri(encodedUri, UriKind.Relative);
	}

	private static Uri MakeUri(string pathSegment, IDictionary<string, string> queryParameters)
	{
		string queryString = string.Join("&",
			queryParameters
				.Select(parameter => $"{HttpUtility.UrlEncode(parameter.Key)}={HttpUtility.UrlEncode(parameter.Value)}"));

		string encodedUri = $"{string.Join("/", HttpUtility.UrlEncode(pathSegment))}?{queryString}";

		return new Uri(encodedUri, UriKind.Relative);
	}

	private async IAsyncEnumerable<MessageBrokerEvent> StreamingGet(Uri uri,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		Console.Out.WriteLine(uri);

		using HttpClient client = _clientFactory.CreateClient(nameof(HttpMessageBroker));
		using HttpRequestMessage httpRequestMessage = new()
		{
			Method = HttpMethod.Get,
			RequestUri = uri,
			Content = new ByteArrayContent(Array.Empty<byte>())
			{
				Headers =
				{
					ContentType = new MediaTypeHeaderValue("application/json")
				}
			}
		};
		using HttpResponseMessage httpResponseMessage = await client
			.SendAsync(httpRequestMessage, cancellationToken)
			.ConfigureAwait(false);
		httpResponseMessage.EnsureSuccessStatusCode();

		await using Stream stream = await httpResponseMessage.Content
			.ReadAsStreamAsync(cancellationToken)
			.ConfigureAwait(false);
		using StreamReader streamReader = new(stream);
		while (!cancellationToken.IsCancellationRequested && !streamReader.EndOfStream)
		{
			string? line = await streamReader.ReadLineAsync().ConfigureAwait(false);
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			MessageBrokerEventDto? messageBrokerEvent = JsonSerializer.Deserialize<MessageBrokerEventDto>(line, _defaultSerializerOptions);
			if (messageBrokerEvent is not null)
			{
				yield return new MessageBrokerEvent(messageBrokerEvent.EventId,
					DateTimeOffset.FromUnixTimeMilliseconds(messageBrokerEvent.CreatedAt),
					Array.Empty<byte>(),
					default);
			}
		}
	}
}
