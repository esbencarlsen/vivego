using System;
using System.Buffers;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace vivego.logger.core
{
	public sealed class TextContentFormatter : IContentFormatter
	{
		private readonly int _maxContentLength;
		private readonly IMediaTypeEncodingResolver _mediaTypeEncodingResolver;
		private readonly ILogger<TextContentFormatter> _logger;

		public TextContentFormatter(int maxContentLength,
			IMediaTypeEncodingResolver mediaTypeEncodingResolver,
			ILogger<TextContentFormatter> logger)
		{
			if (maxContentLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxContentLength));
			_maxContentLength = maxContentLength;
			_mediaTypeEncodingResolver = mediaTypeEncodingResolver ?? throw new ArgumentNullException(nameof(mediaTypeEncodingResolver));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<string> Format(
			string contentType,
			Stream stream,
			CancellationToken cancellationToken)
		{
			if (stream is null) throw new ArgumentNullException(nameof(stream));

			Encoding encoding;
			if (string.IsNullOrEmpty(contentType))
			{
				encoding = Encoding.UTF8;
			}
			else
			{
				try
				{
					encoding = MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed)
						? _mediaTypeEncodingResolver.GetEncoding(parsed.CharSet ?? Encoding.UTF8.WebName)
						: Encoding.UTF8;
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error while parsing content type: {ContentType}; Defaulting to UTF8", contentType);
					encoding = Encoding.UTF8;
				}
			}

			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			using StreamReader streamReader = new(stream, encoding, true, 1024, true);
			using IMemoryOwner<char> memoryOwner = MemoryPool<char>.Shared.Rent(_maxContentLength);
			int bytesRead = await streamReader.ReadAsync(memoryOwner.Memory, cancellationToken).ConfigureAwait(false);
			string decoded = memoryOwner.Memory[..Math.Min(_maxContentLength, bytesRead)].ToString();
			if (bytesRead >= _maxContentLength)
			{
				return decoded + "...";
			}

			return decoded;
		}
	}
}
