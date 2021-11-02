using System;

namespace vivego.logger.web
{
	[Flags]
	public enum RecordOptions
	{
		None = 0,
		RecordRequestBody = 1,
		RecordResponseBody = 2
	}
}