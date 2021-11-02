using System;
using System.Threading.Tasks;

using Cassandra;
using Cassandra.Data.Linq;

namespace vivego.KeyValue.Cassandra;

public static class TableExtensions
{
	public static async Task<bool> CreateIfNotExistsEx<T>(this Table<T> table)
	{
		if (table is null) throw new ArgumentNullException(nameof(table));
		try
		{
			CqlQuerySingleElement<T> querySingleElement = table
				.Take(1)
				.FirstOrDefault();
			querySingleElement
				.DisableTracing()
				.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
			await querySingleElement
				.ExecuteAsync()
				.ConfigureAwait(false);
			return false;
		}
		catch
		{
			await table.CreateIfNotExistsAsync().ConfigureAwait(false);
		}

		return true;
	}
}
