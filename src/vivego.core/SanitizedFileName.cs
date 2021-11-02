using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace vivego.core
{
	public static class SanitizedFileName
	{
		private static readonly HashSet<char> s_badChars = new(Path.GetInvalidFileNameChars());

		public static string Sanitize(string fileName, char replacement = '_')
		{
			if (fileName is null) throw new ArgumentNullException(nameof(fileName));
			if (fileName.Length == 0) return string.Empty;

			StringBuilder sb = new(fileName.Length);
			foreach (char @char in fileName)
			{
				if (s_badChars.Contains(@char))
				{
					sb.Append(replacement);
					continue;
				}

				sb.Append(@char);
			}

			return sb.ToString();
		}
	}
}