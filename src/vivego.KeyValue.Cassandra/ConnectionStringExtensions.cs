using System;

namespace vivego.KeyValue.Cassandra;

public static class ConnectionStringExtensions
{
	public static bool IsAmazonKeyspacesConnection(this string connectionString)
	{
		if (connectionString is null) throw new ArgumentNullException(nameof(connectionString));
		return connectionString.Contains("amazonaws", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsNativeCassandraConnection(this string connectionString)
	{
		if (connectionString is null) throw new ArgumentNullException(nameof(connectionString));
		return !IsAmazonKeyspacesConnection(connectionString);
	}

	public static bool SupportsTtl(this string connectionString)
	{
		if (connectionString is null) throw new ArgumentNullException(nameof(connectionString));
		return IsNativeCassandraConnection(connectionString);
	}
}
