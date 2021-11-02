namespace vivego.Microsoft.Faster
{
	public sealed class CacheContext
	{
		public int Type1 { get; }
		public long Ticks { get; }

		//public static bool operator ==(CacheContext left, CacheContext right)
		//{
		//	if (left is null) throw new ArgumentNullException(nameof(left));
		//	if (right is null) throw new ArgumentNullException(nameof(right));
		//	return left.Equals(right);
		//}

		//public static bool operator !=(CacheContext left, CacheContext right)
		//{
		//	if (left is null) throw new ArgumentNullException(nameof(left));
		//	if (right is null) throw new ArgumentNullException(nameof(right));
		//	return !(left == right);
		//}
	}
}
