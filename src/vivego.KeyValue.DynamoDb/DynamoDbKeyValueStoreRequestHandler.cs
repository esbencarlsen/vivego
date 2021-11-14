using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

using Google.Protobuf;

using MediatR;

using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

using DeleteRequest = vivego.KeyValue.Delete.DeleteRequest;

namespace vivego.KeyValue.DynamoDb
{
	public sealed class DynamoDbKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly DynamoDbKeyValueStoreRequestHandlerConfig _config;
		private readonly IAmazonDynamoDB _amazonDynamoDb;
		private readonly Task<KeyValueStoreFeatures> _features;
		private readonly Lazy<Task<Table>> _lazyInitTask;
		private readonly DeleteItemOperationConfig _deleteItemOperationConfig = new()
		{
			ReturnValues = ReturnValues.None
		};

		public DynamoDbKeyValueStoreRequestHandler(
			DynamoDbKeyValueStoreRequestHandlerConfig config,
			IAmazonDynamoDB amazonDynamoDb)
		{
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_amazonDynamoDb = amazonDynamoDb ?? throw new ArgumentNullException(nameof(amazonDynamoDb));

			_lazyInitTask = new Lazy<Task<Table>>(EnsureTable, true);

			_features = Task.FromResult(new KeyValueStoreFeatures
			{
				SupportsEtag = config.SupportsEtag,
				SupportsTtl = true,
				MaximumDataSize = 1024L * 1024L * 1024L, // 1GB
				MaximumKeyLength = 1024
			});
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			return _features;
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry(!_config.SupportsEtag);
			Document document = new()
			{
				["Id"] = request.Entry.Key,
				["Data"] = keyValueEntry.ToByteArray(),
				["ETag"] = keyValueEntry.ETag
			};

			if (request.Entry.ExpiresInSeconds > 0)
			{
				document["ExpiresAt"] = DateTimeOffset.UtcNow.AddSeconds(request.Entry.ExpiresInSeconds).ToUnixTimeSeconds();
			}

			Table table = await _lazyInitTask.Value.ConfigureAwait(false);
			if (!string.IsNullOrEmpty(request.Entry.ETag))
			{
				Expression expr = new()
				{
					ExpressionStatement = "ETag = :val",
					ExpressionAttributeValues = { [":val"] = request.Entry.ETag }
				};

				UpdateItemOperationConfig config = new()
				{
					ConditionalExpression = expr,
					ReturnValues = ReturnValues.None
				};

				try
				{
					await table.UpdateItemAsync(document, config, cancellationToken).ConfigureAwait(false);
				}
				catch (ConditionalCheckFailedException)
				{
					return string.Empty;
				}

				return keyValueEntry.ETag;
			}

			await table.PutItemAsync(document, cancellationToken).ConfigureAwait(false);

			return keyValueEntry.ETag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));

			GetItemOperationConfig config = new()
			{
				AttributesToGet = new List<string> { "Id", "Data", "ETag" },
				ConsistentRead = true
			};

			Table table = await _lazyInitTask.Value.ConfigureAwait(false);

			try
			{
				Document? document = await table
					.GetItemAsync(request.Key, config, cancellationToken)
					.ConfigureAwait(false);

				if (document is null
					|| !document.TryGetValue("Data", out DynamoDBEntry? data))
				{
					return KeyValueEntryExtensions.KeyValueNull;
				}

				KeyValueEntry entry = KeyValueEntry.Parser.ParseFrom(data.AsByteArray());

				if (entry.ExpiresAtUnixTimeSeconds > 0
					&& DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(entry.ExpiresAtUnixTimeSeconds))
				{
					return KeyValueEntryExtensions.KeyValueNull;
				}

				return entry;
			}
			catch (ResourceNotFoundException)
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Entry.Key));

			Table table = await _lazyInitTask.Value.ConfigureAwait(false);

			if (_config.SupportsEtag
				&& !string.IsNullOrEmpty(request.Entry.ETag))
			{
				Expression expr = new()
				{
					ExpressionStatement = "ETag = :val",
					ExpressionAttributeValues = { [":val"] = request.Entry.ETag }
				};
				DeleteItemOperationConfig deleteItemOperationConfig = new()
				{
					ConditionalExpression = expr,
					ReturnValues = ReturnValues.None
				};
				try
				{
					await table
						.DeleteItemAsync(request.Entry.Key, deleteItemOperationConfig, cancellationToken)
						.ConfigureAwait(false);
				}
				catch (ConditionalCheckFailedException)
				{
					return false;
				}
			}
			else
			{
				await table
					.DeleteItemAsync(request.Entry.Key, _deleteItemOperationConfig, cancellationToken)
					.ConfigureAwait(false);
			}

			return true;
		}

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		private async Task<Table> EnsureTable()
		{
			try
			{
				await _amazonDynamoDb
					.DescribeTableAsync(_config.TableName, CancellationToken.None)
					.ConfigureAwait(false);
				return Table.LoadTable(_amazonDynamoDb, _config.TableName);
			}
			catch
			{
				// Ignore
			}


			CreateTableRequest request = new()
			{
				BillingMode = BillingMode.PAY_PER_REQUEST,
				TableName = _config.TableName,
				AttributeDefinitions = new List<AttributeDefinition>
				{
					new()
					{
						AttributeName = "Id",
						AttributeType = "S"
					}
				},
				KeySchema = new List<KeySchemaElement>
				{
					new()
					{
						AttributeName = "Id",
						KeyType = KeyType.HASH
					}
				}
			};

			try
			{
				await _amazonDynamoDb
					.CreateTableAsync(request)
					.ConfigureAwait(false);
			}
			catch (ResourceInUseException)
			{
				// Ignore
			}

			return Table.LoadTable(_amazonDynamoDb, _config.TableName);
		}
	}
}
