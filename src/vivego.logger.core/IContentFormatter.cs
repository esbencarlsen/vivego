using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.logger.core
{
	public interface IContentFormatter
	{
		Task<string> Format(string contentType,
			Stream stream,
			CancellationToken cancellationToken);
	}
}