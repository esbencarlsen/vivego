using System.Collections.Generic;
using System.Text;

namespace vivego.KeyValue.S3
{
	internal class KeyHelper
	{
		public static KeyHelper Instance = new();
		private readonly HashSet<char> _validCharacters;

		private KeyHelper()
		{
			const string validCharacters = "!-_.*'()0123456789qwertyuioplkjhgfdsazxcvbnmQWERTYUIOPLKJHGFDSAZXCVBNM";
			_validCharacters = new HashSet<char>(validCharacters.ToCharArray());
		}

		public string MakeValidKey(string key)
		{
			StringBuilder stringBuilder = new(key.Length);
			foreach (char c in key)
			{
				stringBuilder.Append(_validCharacters.Contains(c) ? c : '_');
			}

			return stringBuilder.ToString();
		}
	}
}