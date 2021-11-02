using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Http.Model;

#pragma warning disable SCS0012
namespace vivego.KeyValue.Http
{
	[ApiController]
	[Route("kv")]
	public sealed class HttpServerControllerKeyValueStore : ControllerBase
	{
		private readonly IKeyValueStore _keyValueStore;

		public HttpServerControllerKeyValueStore(IKeyValueStore keyValueStore) => _keyValueStore =
			keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));

		[HttpPost("{key}")]
		[Produces("application/json")]
		public async ValueTask<IActionResult> Set(
			[FromRoute] [Required] [MinLength(1)] [MaxLength(255)]
			string key,
			[FromQuery] [MinLength(1)] [MaxLength(255)]
			string? etag = default,
			[FromQuery] long expiresInSeconds = 0)
		{
			Request.EnableBuffering();
			ByteString content = await ByteString
				.FromStreamAsync(Request.Body)
				.ConfigureAwait(false);

			HttpContent httpContent = new()
			{
				ContentType = Request.ContentType,
				Data = content
			};

			SetKeyValueEntry setKeyValueEntry = new()
			{
				Key = key,
				ETag = etag ?? string.Empty,
				ExpiresInSeconds = expiresInSeconds,
				Value = httpContent.ToNullableBytes()
			};

			string newEtag = await _keyValueStore
				.Set(setKeyValueEntry)
				.ConfigureAwait(false);
			if (string.IsNullOrEmpty(newEtag))
			{
				return Conflict("etag does not match");
			}

			return Ok(newEtag);
		}

		[HttpGet("{key}")]
		public async ValueTask<IActionResult> Get([FromRoute] [Required] [MinLength(1)] [MaxLength(255)] string key)
		{
			KeyValueEntry keyValueEntry = await _keyValueStore
				.Get(key, HttpContext.RequestAborted)
				.ConfigureAwait(false);
			if (string.IsNullOrEmpty(keyValueEntry.ETag))
			{
				return NoContent();
			}

			HttpContent httpContent = HttpContent.Parser.ParseFrom(keyValueEntry.Value.Data);
			Response.Headers.Add("Etag", keyValueEntry.ETag);
			if (keyValueEntry.ExpiresAtUnixTimeSeconds > 0)
			{
				Response.Headers.Add("ExpiresAt", DateTimeOffset.FromUnixTimeSeconds(keyValueEntry.ExpiresAtUnixTimeSeconds).ToString("O", CultureInfo.InvariantCulture));
			}

			return File(httpContent.Data.ToByteArray(), string.IsNullOrEmpty(httpContent.ContentType)
				? "application/octet-stream"
				: httpContent.ContentType);
		}

		[HttpGet("features")]
		public ValueTask<KeyValueStoreFeatures> GetFeatures() => _keyValueStore.GetFeatures(HttpContext.RequestAborted);

		[HttpDelete("{key}")]
		[Produces("application/json")]
		public async ValueTask<IActionResult> Delete(
			[FromRoute] [Required] [MinLength(1)] [MaxLength(255)]
			string key,
			[FromQuery] [MinLength(1)] [MaxLength(255)]
			string? etag = default)
		{
			bool deleted = await _keyValueStore
				.Delete(new DeleteKeyValueEntry
				{
					Key = key,
					ETag = etag ?? string.Empty
				}, HttpContext.RequestAborted)
				.ConfigureAwait(false);
			if (deleted)
			{
				return Ok();
			}

			return Conflict("Not deleted");
		}

		[HttpPut]
		[Produces("application/json")]
		public async ValueTask<IActionResult> Clear()
		{
			await _keyValueStore.Clear(HttpContext.RequestAborted).ConfigureAwait(false);
			return Ok();
		}
	}
}
