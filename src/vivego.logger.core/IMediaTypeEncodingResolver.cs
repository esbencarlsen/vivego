using System.Text;

namespace vivego.logger.core
{
	public interface IMediaTypeEncodingResolver
	{
		Encoding GetEncoding(string charSet);
	}
}