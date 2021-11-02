using Orleans.Concurrency;

namespace vivego.Orleans.ReactiveCache;

/// <summary>
/// This class illustrates a version token with a concept of "no version".
/// We can use this explicit "no version" value to notify reactive pollers that "no new version" of the value has become available within the allotted time.
/// We can also pair this "no version" with a null or otherwise empty return value to avoid putting unnecessary data on the network, such as a timeout exception.
/// This is just a generic example - we can use anything as a version that makes sense to the use case.
/// </summary>
[Immutable]
public readonly record struct VersionToken
{
	private readonly uint _token;

	private VersionToken(uint token)
	{
		_token = token;
	}

	/// <summary>
	/// Returns the next version after the current one.
	/// While the underlying integer cycles, it skips zero.
	/// This is because zero is reserved to mean "no version".
	/// </summary>
	public VersionToken Next() => new(_token == uint.MaxValue ? 1 : _token + 1);

	/// <summary>
	/// Returns the "no version" starting point, which means there is no data at all.
	/// Call <see cref="Next()"/> on this if you want to issue a proper version token.
	/// </summary>
	public static VersionToken None { get; } = new(0);
}
