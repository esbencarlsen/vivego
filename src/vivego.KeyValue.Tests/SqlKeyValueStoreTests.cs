using System;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Sql;

namespace vivego.KeyValue.Tests
{
	public class SqlKeyValueStoreTests : KeyValueStoreTests, IDisposable
	{
		private const string InMemoryConnectionString = "DataSource=:memory:";
		private SqliteConnection? _connection;

		protected virtual void Dispose(bool disposing)
		{
			_connection?.Close();
		}

		public void Dispose()
		{
			// Dispose of unmanaged resources.
			Dispose(true);
			// Take yourself off the finalization queue
			// to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}

		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			_connection = new SqliteConnection(InMemoryConnectionString);
			_connection.Open();
			serviceCollection.AddSqlKeyValueStore("UnitTest",
				"unittest",
				(_, tableName, builder) =>
				{
					builder.UseSqlite(_connection);
					using StateDbContext context = new(tableName, builder.Options);
					context.Database.EnsureCreated();
					RelationalDatabaseCreator creator = (RelationalDatabaseCreator)context.Database.GetService<IRelationalDatabaseCreator>();
					try
					{
						creator.CreateTables();
					}
					catch
					{
					}
				});
		}
	}
}
