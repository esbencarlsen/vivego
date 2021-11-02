using Orleans.Concurrency;

namespace vivego.Orleans.ReactiveCache;

[Immutable]
public sealed class VersionedValue<T>
{
	public VersionedValue(VersionToken version, T? value)
	{
		Version = version;
		Value = value;
	}

	public VersionToken Version { get; }
	public T? Value { get; }

	/// <summary>
	/// True if the current version is different from <see cref="VersionToken.None"/>, otherwise false.
	/// </summary>
	public bool IsValid => Version != VersionToken.None;

	public VersionedValue<T> NextVersion(T? value) => new(Version.Next(), value);

#pragma warning disable CA1000
	public static VersionedValue<T> None { get; } = new(VersionToken.None, default);
#pragma warning restore CA1000
}
