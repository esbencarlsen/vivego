using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Orleans.KeyValueProvider.Storage
{
	public sealed class KeyValueStoreGrainStorage : IGrainStorage
	{
		private readonly IKeyValueStore _keyValueStore;
		private readonly SerializationManager _serializationManager;
		private readonly ILogger<KeyValueStoreGrainStorage> _logger;
		private readonly KeyValueStoreStorageOptions _keyValueStoreStorageOptions;
		private readonly Lazy<Task<KeyValueStoreFeatures>> _keyValueStoreFeatures;

		public KeyValueStoreGrainStorage(
			IKeyValueStore keyValueStore,
			SerializationManager serializationManager,
			ILogger<KeyValueStoreGrainStorage> logger,
			KeyValueStoreStorageOptions keyValueStoreStorageOptions)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
			_serializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_keyValueStoreStorageOptions = keyValueStoreStorageOptions ?? throw new ArgumentNullException(nameof(keyValueStoreStorageOptions));

			_keyValueStoreFeatures = new Lazy<Task<KeyValueStoreFeatures>>(() => _keyValueStore.GetFeatures().AsTask(), LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
		{
			if (grainReference is null) throw new ArgumentNullException(nameof(grainReference));
			if (grainState is null) throw new ArgumentNullException(nameof(grainState));
			if (string.IsNullOrEmpty(grainType)) throw new ArgumentException("Value cannot be null or empty.", nameof(grainType));

			string key = MakeKey(grainReference);

			KeyValueEntry keyValueEntry = await _keyValueStore
				.Get(key, CancellationToken.None)
				.ConfigureAwait(false);

			bool keyValueStoreSupportsETag = (await _keyValueStoreFeatures.Value.ConfigureAwait(false)).SupportsEtag;
			if (keyValueStoreSupportsETag
				&& _keyValueStoreStorageOptions.SupportsETag
				&& string.IsNullOrEmpty(keyValueEntry.ETag)
				|| keyValueEntry.Value.Data.IsEmpty)
			{
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebug("Tried to load state for grain '{Key}', but there was no data", key);
				}

				grainState.RecordExists = false;
				return;
			}

			grainState.State = _serializationManager.Deserialize(grainState.Type, new BinaryTokenStreamReader(keyValueEntry.Value.Data.ToByteArray()));
			grainState.ETag = keyValueEntry.ETag;
			grainState.RecordExists = true;
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Loaded grain state for '{Key}', eTag: '{ETag}'", key, grainState.ETag);
			}
		}

		public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
		{
			if (grainReference is null) throw new ArgumentNullException(nameof(grainReference));
			if (grainState is null) throw new ArgumentNullException(nameof(grainState));
			if (string.IsNullOrEmpty(grainType)) throw new ArgumentException("Value cannot be null or empty.", nameof(grainType));

			byte[] serialized = _serializationManager.SerializeToByteArray(grainState.State);
			string key = MakeKey(grainReference);
			grainState.ETag = await _keyValueStore
				.Set(key, serialized, timeToLive: _keyValueStoreStorageOptions.Ttl)
				.ConfigureAwait(false);

			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Saved grain state for '{Key}', eTag: '{ETag}'", key, grainState.ETag);
			}
		}

		public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
		{
			if (grainReference is null) throw new ArgumentNullException(nameof(grainReference));
			if (grainState is null) throw new ArgumentNullException(nameof(grainState));
			if (string.IsNullOrEmpty(grainType)) throw new ArgumentException("Value cannot be null or empty.", nameof(grainType));

			string key = MakeKey(grainReference);
			bool success = await _keyValueStore
				.DeleteEntry(key, string.Empty)
				.ConfigureAwait(false);
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				if (success)
				{
					_logger.LogDebug("Cleared grain state for '{Key}', eTag: '{ETag}'", key, grainState.ETag);
				}
				else
				{
					_logger.LogDebug("Tried to clear grain state for '{Key}', eTag: '{ETag}' but there was nothing to clear", key, grainState.ETag);
				}
			}
		}

		private static string MakeKey(GrainReference grainReference)
		{
			return grainReference.ToShortKeyString();
		}

		public static IGrainStorage Create(IServiceProvider serviceProvider,
			string providerName,
			Func<IServiceProvider, IKeyValueStore>? keyValueStoreFactory = default)
		{
			IOptionsMonitor<KeyValueStoreStorageOptions> eventMassTableStorageOptions = serviceProvider
				.GetRequiredService<IOptionsMonitor<KeyValueStoreStorageOptions>>();

			IKeyValueStore keyValueStore = keyValueStoreFactory is null
				? serviceProvider.GetRequiredService<IKeyValueStore>()
				: keyValueStoreFactory(serviceProvider);

			return ActivatorUtilities.CreateInstance<KeyValueStoreGrainStorage>(serviceProvider,
				eventMassTableStorageOptions.Get(providerName),
				keyValueStore);
		}
	}
}
