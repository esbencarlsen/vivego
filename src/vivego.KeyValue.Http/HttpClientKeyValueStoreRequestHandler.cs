using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.Http
{
	public sealed class HttpClientKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly IHttpClientFactory _clientFactory;

		public HttpClientKeyValueStoreRequestHandler(IHttpClientFactory clientFactory)
		{
			_clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
		}

		private KeyValueStoreFeatures? _features;
		public async Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			if (_features is null)
			{
				using HttpClient client = _clientFactory.CreateClient(nameof(HttpClientKeyValueStoreRequestHandler));
				using HttpResponseMessage httpResponseMessage = await client
					.GetAsync(new Uri("features", UriKind.Relative), cancellationToken)
					.ConfigureAwait(false);
				httpResponseMessage.EnsureSuccessStatusCode();
				string serializedContent = await httpResponseMessage.Content
					.ReadAsStringAsync(cancellationToken)
					.ConfigureAwait(false);
				_features = JsonSerializer.Deserialize<KeyValueStoreFeatures>(serializedContent)!;
			}

			return _features;
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			UriBuilder uriBuilder = new() { Path = request.Entry.Key };
			if (!string.IsNullOrEmpty(request.Entry.ETag))
			{
				uriBuilder.Query = request.Entry.ETag;
			}

			using HttpClient client = _clientFactory.CreateClient(nameof(HttpClientKeyValueStoreRequestHandler));
			using ByteArrayContent content = new(request.Entry.Value.ToBytes() ?? Array.Empty<byte>());
			using HttpResponseMessage httpResponseMessage = await client
				.PostAsync(new Uri(request.Entry.Key, UriKind.Relative), content, cancellationToken)
				.ConfigureAwait(false);

			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return string.Empty;
			}

			string etag = GetEtag(httpResponseMessage);
			return etag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

			using HttpClient client = _clientFactory.CreateClient(nameof(HttpClientKeyValueStoreRequestHandler));
			using HttpResponseMessage httpResponseMessage = await client
				.GetAsync(new Uri(request.Key, UriKind.Relative), cancellationToken)
				.ConfigureAwait(false);

			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return new KeyValueEntry
				{
					ETag = string.Empty,
					Value = NullableBytesExtensions.EmptyNullableBytes,
					ExpiresAtUnixTimeSeconds = 0
				};
			}

			byte[] content = await httpResponseMessage.Content
				.ReadAsByteArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			string etag = GetEtag(httpResponseMessage);
			return new KeyValueEntry
			{
				ETag = etag,
				Value = content.ToNullableBytes(),
				ExpiresAtUnixTimeSeconds = 0
			};
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			UriBuilder uriBuilder = new() { Path = request.Entry.Key };
			if (!string.IsNullOrEmpty(request.Entry.ETag))
			{
				uriBuilder.Query = request.Entry.ETag;
			}

			using HttpClient client = _clientFactory.CreateClient(nameof(HttpClientKeyValueStoreRequestHandler));
			using HttpResponseMessage httpResponseMessage = await client
				.DeleteAsync(uriBuilder.Uri, cancellationToken)
				.ConfigureAwait(false);
			return httpResponseMessage.IsSuccessStatusCode;
		}

		public async Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			using HttpClient client = _clientFactory.CreateClient(nameof(HttpClientKeyValueStoreRequestHandler));
			using ByteArrayContent nullContent = new(Array.Empty<byte>());
			using HttpResponseMessage httpResponseMessage = await client
				.PutAsync(new Uri("/", UriKind.Relative), nullContent, cancellationToken)
				.ConfigureAwait(false);
			return Unit.Value;
		}

		private static string GetEtag(HttpResponseMessage httpResponseMessage)
		{
			if (httpResponseMessage is null) throw new ArgumentNullException(nameof(httpResponseMessage));
			if (httpResponseMessage.Headers.TryGetValues("etag", out IEnumerable<string>? etagCollection))
			{
				return etagCollection.FirstOrDefault() ?? string.Empty;
			}

			return string.Empty;
		}
	}
}
