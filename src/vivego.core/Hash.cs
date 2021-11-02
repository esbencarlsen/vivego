using System;

namespace vivego.core
{
	public static class Hash
	{
		public static int GetDeterministicHashCode(this string str)
		{
			if (str is null) throw new ArgumentNullException(nameof(str));
			unchecked
			{
				int hash1 = (5381 << 16) + 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1)
					{
						break;
					}

					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return Math.Abs(hash1 + hash2 * 1566083941);
			}
		}
		
		public static int GetDeterministicHashCode(this byte[] data)
		{
			if (data is null) throw new ArgumentNullException(nameof(data));
			unchecked
			{
				int hash1 = (5381 << 16) + 5381;
				int hash2 = hash1;

				for (int i = 0; i < data.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ data[i];
					if (i == data.Length - 1)
					{
						break;
					}

					hash2 = ((hash2 << 5) + hash2) ^ data[i + 1];
				}

				return Math.Abs(hash1 + hash2 * 1566083941);
			}
		}
	}
}