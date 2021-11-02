using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using vivego.core;

namespace vivego.KeyValue.Cassandra;

public static class CqlCommandExtensions
{
	public static Task<T> LogAndExecuteAsync<T>(this CqlScalar<T> cqlScalar, ILogger logger)
	{
		if (cqlScalar is null) throw new ArgumentNullException(nameof(cqlScalar));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return LogAndExecuteAsync(cqlScalar.ExecuteAsync, logger, () =>
			("SELECT SCALAR", cqlScalar.ToString(), cqlScalar.GetTable().Name, cqlScalar.GetTable().KeyspaceName, cqlScalar.QueryValues));
	}

	public static Task<RowSet> LogAndExecuteAsync(this CqlUpdate update, ILogger logger)
	{
		if (update is null) throw new ArgumentNullException(nameof(update));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return update.LogAndExecuteAsync(logger, "UPDATE");
	}

	public static Task<AppliedInfo<T>> LogAndExecuteAsync<T>(this CqlConditionalCommand<T> command, ILogger logger)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return command.LogAndExecuteAsync(logger, "UPDATE/INSERT IF");
	}

	public static Task<RowSet> LogAndExecuteAsync(this CqlDelete delete, ILogger logger)
	{
		if (delete is null) throw new ArgumentNullException(nameof(delete));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return delete.LogAndExecuteAsync(logger, "DELETE");
	}

	public static Task<T> LogAndExecuteAsync<T>(this CqlQuerySingleElement<T> selectSingle, ILogger logger)
	{
		if (selectSingle is null) throw new ArgumentNullException(nameof(selectSingle));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return selectSingle.LogAndExecuteAsync(logger, "SELECT SINGLE");
	}

	public static Task<RowSet> LogAndExecuteAsync(this ISession session, ILogger logger, IStatement statement, Func<string> cqlFunc)
	{
		if (session is null) throw new ArgumentNullException(nameof(session));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (statement is null) throw new ArgumentNullException(nameof(statement));
		if (cqlFunc is null) throw new ArgumentNullException(nameof(cqlFunc));
		return LogAndExecuteAsync(() => session.ExecuteAsync(statement), logger, () =>
			("GENERIC", cqlFunc(), "NA", session.Keyspace, statement.QueryValues));
	}

	public static Task<IEnumerable<T>> LogAndExecuteAsync<T>(this CqlQueryBase<T> query, ILogger logger)
	{
		if (query is null) throw new ArgumentNullException(nameof(query));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		return LogAndExecuteAsync(query.ExecuteAsync, logger, () =>
			("GENERIC", query.ToString(), query.GetTable().Name, query.GetTable().KeyspaceName ?? query.Keyspace, query.QueryValues));
	}

#pragma warning disable CA2211 // Non-constant fields should not be visible
	// ReSharper disable once MemberCanBePrivate.Global
	public static readonly long WarningThresholdInMs = 500;
#pragma warning restore CA2211 // Non-constant fields should not be visible

	private static async Task<T> LogAndExecuteAsync<T>(this Func<Task<T>> action,
		ILogger logger,
		Func<(string statementType,
			string? cql,
			string tableName,
			string keyspace,
			IEnumerable<object> parameters)> loggerPropertiesFunc)
	{
		if (action is null) throw new ArgumentNullException(nameof(action));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (loggerPropertiesFunc is null) throw new ArgumentNullException(nameof(loggerPropertiesFunc));
		Stopwatch sw = Stopwatch.StartNew();
		try
		{
			T result = await action().ConfigureAwait(false);
			sw.Stop();

			if (sw.ElapsedMilliseconds >= WarningThresholdInMs)
			{
				if (logger.IsEnabled(LogLevel.Warning))
				{
					(string statementType, string? cql, string tableName, string keyspace, IEnumerable<object> queryValues) = loggerPropertiesFunc();
					logger.LogWarning(
						"Executing CQL {StatementType} statement '{Cql}' on table {TableName} in keyspace {Keyspace} in {ExecutionTime} ms; QueryValues: {QueryValues}",
						statementType, cql, tableName, keyspace, sw.ElapsedMilliseconds, FormatParameters(queryValues));
				}
			}
			else
			{
				if (logger.IsEnabled(LogLevel.Information))
				{
					(string statementType, string? cql, string tableName, string keyspace, IEnumerable<object> queryValues) = loggerPropertiesFunc();
					logger.LogInformation(
						"Executing CQL {StatementType} statement '{Cql}' on table {TableName} in keyspace {Keyspace} in {ExecutionTime} ms; QueryValues: {QueryValues}",
						statementType, cql, tableName, keyspace, sw.ElapsedMilliseconds, FormatParameters(queryValues));
				}
			}

			return result;
		}
		catch (Exception exception)
		{
			sw.Stop();
			(string statementType, string? cql, string tableName, string keyspace, IEnumerable<object> queryValues) = loggerPropertiesFunc();
			logger.LogError(new EventId(),
				exception,
				"Executing CQL {StatementType} statement '{Cql}' on table {TableName} in keyspace {Keyspace} in {ExecutionTime} ms; QueryValues: {QueryValues}",
				statementType, cql, tableName, keyspace, sw.ElapsedMilliseconds, FormatParameters(queryValues));
			throw;
		}
	}

	public static Task<RowSet> LogAndExecuteAsync(this CqlCommand command,
		ILogger logger,
		string statementType)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (statementType is null) throw new ArgumentNullException(nameof(statementType));
		return LogAndExecuteAsync(command.ExecuteAsync, logger, () =>
			(statementType, command.QueryString, command.GetTable().Name, command.GetTable().KeyspaceName, command.QueryValues));
	}

	private static Task<AppliedInfo<T>> LogAndExecuteAsync<T>(this CqlConditionalCommand<T> command,
		ILogger logger,
		string statementType)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (statementType is null) throw new ArgumentNullException(nameof(statementType));
		return LogAndExecuteAsync(command.ExecuteAsync, logger, () =>
			(statementType, command.QueryString, command.GetTable().Name, command.GetTable().KeyspaceName, command.QueryValues));
	}

	private static Task<T> LogAndExecuteAsync<T>(this CqlQuerySingleElement<T> query,
		ILogger logger,
		string statementType)
	{
		return LogAndExecuteAsync(query.ExecuteAsync, logger, () =>
			(statementType, query.ToString(), query.GetTable().Name, query.GetTable().KeyspaceName, query.QueryValues));
	}

	private static string FormatParameters(IEnumerable<object?>? source)
	{
		StringBuilder stringBuilder = new();
		foreach (object? o in source.EmptyIfNull())
		{
			if (o is null)
			{
				continue;
			}

			ReadOnlySpan<char> serialized = JsonConvert.SerializeObject(o).AsSpan();
			if (serialized.IsEmpty)
			{
				continue;
			}

			if (serialized.Length > 300)
			{
				stringBuilder.Append(serialized[..300]);
				stringBuilder.Append("...");
			}
			else
			{
				stringBuilder.Append(serialized);
			}

			stringBuilder.Append(", ");
		}

		return stringBuilder.ToString();
	}
}
