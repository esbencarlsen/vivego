using System;
using System.Net.Http;

namespace vivego.logger.HttpClient
{
	public static class HttpContentExtensions
	{
		public static HttpContent Copy(this HttpContent httpContent, byte[] bytes)
		{
			if (httpContent is null) throw new ArgumentNullException(nameof(httpContent));

			ByteArrayContent content = new(bytes);
			foreach (var (s, value) in httpContent.Headers)
			{
				content.Headers.TryAddWithoutValidation(s, value);
			}

			return content;
		}
	}
}