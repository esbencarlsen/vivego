using System;

using FASTER.core;

using vivego.core;

namespace vivego.Microsoft.Faster
{
	public sealed class StringKey : IFasterEqualityComparer<StringKey>
	{
		public string Key { get; }

		public StringKey(string key) => Key = key ?? throw new ArgumentNullException(nameof(key));

		public long GetHashCode64(ref StringKey key)
		{
			if (key is null) throw new ArgumentNullException(nameof(key));
			return key.Key.GetDeterministicHashCode();
		}

		public bool Equals(ref StringKey k1, ref StringKey k2)
		{
#pragma warning disable CA1062 // Validate arguments of public methods
			return k1.Key.Equals(k2.Key, StringComparison.Ordinal);
#pragma warning restore CA1062 // Validate arguments of public methods
		}

#pragma warning disable CA2225 // Operator overloads have named alternates
		public static implicit operator StringKey(string first)
#pragma warning restore CA2225 // Operator overloads have named alternates
		{
			return new(first);
		}

		public override string ToString() => $"{nameof(Key)}: {Key}";
	}
}
