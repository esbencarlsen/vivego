using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.logger.core
{
	public sealed class NullContentFormatter : IContentFormatter
	{
		public Task<string> Format(string contentType, Stream stream, CancellationToken cancellationToken) => Task.FromResult<string>(default!);
	}
}
