using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Sql;
using vivego.ServiceBuilder.Abstractions;

// ReSharper disable once CheckNamespace
namespace vivego.KeyValue
{
	public static class KeyValueConfigurationExtensions
	{
		public static IServiceBuilder AddSqlKeyValueStore(this IServiceCollection collection,
			string name,
			string tableName,
			DbContextOptions<StateDbContext> options)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (options is null) throw new ArgumentNullException(nameof(options));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));

			KeyValueStoreBuilder builder = new(name, collection);
			builder.RegisterKeyValueStoreRequestHandler<SqlKeyValueStoreRequestHandler>();
			builder.Services.AddSingleton<Func<StateDbContext>>(() => new StateDbContext(tableName, options));
			builder.Services.AddSingleton<SqlKeyValueStoreRequestHandler>();
			return builder;
		}

		public static IServiceBuilder AddSqlKeyValueStore(this IServiceCollection collection,
			string name,
			string tableName,
			Action<string, string, DbContextOptionsBuilder<StateDbContext>> configure)
		{
			if (collection is null) throw new ArgumentNullException(nameof(collection));
			if (configure is null) throw new ArgumentNullException(nameof(configure));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("Value cannot be null or empty.", nameof(tableName));

			DbContextOptionsBuilder<StateDbContext> contextOptions = new();
			configure(name, tableName, contextOptions);

			return AddSqlKeyValueStore(collection, name, tableName, contextOptions.Options);
		}
	}
}