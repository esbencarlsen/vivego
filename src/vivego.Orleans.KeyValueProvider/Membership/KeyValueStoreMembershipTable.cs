using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Orleans;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;

using vivego.KeyValue;
using vivego.KeyValue.Abstractions.Model;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public sealed class KeyValueStoreMembershipTable : IMembershipTable, IGatewayListProvider
	{
		private readonly IKeyValueStore _keyValueStore;
		private readonly IOptions<ClusterOptions> _clusterOptions;
		private readonly string _membershipKey;

		public KeyValueStoreMembershipTable(
			IKeyValueStore keyValueStore,
			IOptions<ClusterOptions> clusterOptions)
		{
			_keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
			_clusterOptions = clusterOptions ?? throw new ArgumentNullException(nameof(clusterOptions));

			_membershipKey = $"{_clusterOptions.Value.ServiceId}_{nameof(KeyValueStoreMembershipTable)}";
		}

		public async Task InitializeMembershipTable(bool tryInitTableVersion)
		{
			if (tryInitTableVersion)
			{
				TableVersion tableVersion = await Get().ConfigureAwait(false);
				await Set(tableVersion).ConfigureAwait(false);
			}
		}

		public async Task DeleteMembershipTableEntries(string clusterId)
		{
			if (clusterId is null) throw new ArgumentNullException(nameof(clusterId));
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			foreach ((string key, MembershipEntry membershipEntry) in tableVersion.Entries.ToArray())
			{
				if (clusterId.Equals(membershipEntry.ClusterId, StringComparison.Ordinal))
				{
					tableVersion.Entries.Remove(key);
				}
			}

			await Set(tableVersion).ConfigureAwait(false);
		}

		public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
		{
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			foreach ((string key, MembershipEntry membershipEntry) in tableVersion.Entries.ToArray())
			{
				if (membershipEntry.AmAliveTime < beforeDate)
				{
					tableVersion.Entries.Remove(key);
				}
			}

			await Set(tableVersion).ConfigureAwait(false);
		}

		public async Task<MembershipTableData> ReadRow(SiloAddress key)
		{
			if (key is null) throw new ArgumentNullException(nameof(key));
			string id = key.ToParsableString();
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			return new MembershipTableData(
				tableVersion.Entries
					.Where(entry => id.Equals(entry.Key, StringComparison.Ordinal))
					.Select(entry => new Tuple<global::Orleans.MembershipEntry, string>(entry.Value.ToOrleansMembershipEntry(entry.Key), tableVersion.VersionEtag!))
					.ToList(),
				tableVersion.ToOrleansTableVersion());
		}

		public async Task<MembershipTableData> ReadAll()
		{
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			return new MembershipTableData(
				tableVersion.Entries
					.Select(entry => new Tuple<global::Orleans.MembershipEntry, string>(entry.Value.ToOrleansMembershipEntry(entry.Key), tableVersion.VersionEtag!))
					.ToList(),
				tableVersion.ToOrleansTableVersion());
		}

		public async Task<bool> InsertRow(global::Orleans.MembershipEntry entry, global::Orleans.TableVersion orleansTableVersion)
		{
			if (entry is null) throw new ArgumentNullException(nameof(entry));
			if (orleansTableVersion is null) throw new ArgumentNullException(nameof(orleansTableVersion));

			TableVersion tableVersion = await Get().ConfigureAwait(false);
			tableVersion.Version = orleansTableVersion.Version;
			string id = entry.SiloAddress.ToParsableString();
			tableVersion.Entries[id] = entry
				.ToOrleansMembershipEntry(_clusterOptions.Value.ServiceId, _clusterOptions.Value.ClusterId, tableVersion.VersionEtag);
			string tag = await Set(tableVersion).ConfigureAwait(false);
			return !string.IsNullOrEmpty(tag);
		}

		public async Task<bool> UpdateRow(global::Orleans.MembershipEntry entry, string etag, global::Orleans.TableVersion orleansTableVersion)
		{
			if (entry is null) throw new ArgumentNullException(nameof(entry));
			if (orleansTableVersion is null) throw new ArgumentNullException(nameof(orleansTableVersion));

			TableVersion tableVersion = await Get().ConfigureAwait(false);
			tableVersion.Version = orleansTableVersion.Version;
			string id = entry.SiloAddress.ToParsableString();
			tableVersion.Entries[id] = entry
				.ToOrleansMembershipEntry(_clusterOptions.Value.ServiceId, _clusterOptions.Value.ClusterId, tableVersion.VersionEtag);

			string tag = await Set(tableVersion).ConfigureAwait(false);
			return !string.IsNullOrEmpty(tag);
		}

		public async Task UpdateIAmAlive(global::Orleans.MembershipEntry entry)
		{
			if (entry is null) throw new ArgumentNullException(nameof(entry));
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			string id = entry.SiloAddress.ToParsableString();
			if (tableVersion.Entries.TryGetValue(id, out MembershipEntry? membershipEntry))
			{
				tableVersion.Entries[id] = membershipEntry with
				{
					AmAliveTime = entry.IAmAliveTime
				};
			}

			await Set(tableVersion).ConfigureAwait(false);
		}

		public Task InitializeGatewayListProvider()
		{
			return Task.CompletedTask;
		}

		public async Task<IList<Uri>> GetGateways()
		{
			TableVersion tableVersion = await Get().ConfigureAwait(false);
			return tableVersion.Entries
				.Where(pair => pair.Value.Status.Equals(SiloStatus.Active.ToString(), StringComparison.Ordinal))
				.Select(entry =>
				{
					var (key, value) = entry;
					Uri uri = SiloAddress.FromParsableString(key).ToGatewayUri();
					return new UriBuilder(uri.Scheme, uri.Host, value.ProxyPort, "0").Uri;
				})
				.ToList();
		}

		public TimeSpan MaxStaleness { get; } = TimeSpan.FromSeconds(10);
		public bool IsUpdatable => true;

		private async Task<TableVersion> Get()
		{
			KeyValueEntry stateGetResponse = await _keyValueStore
				.Get(_membershipKey, CancellationToken.None)
				.ConfigureAwait(false);

			TableVersion? tableVersion = default;
			if (!string.IsNullOrEmpty(stateGetResponse.ETag)
				&& !stateGetResponse.Value.IsNull())
			{
				tableVersion = stateGetResponse.Value.Data.ToByteArray().Deserialize();
			}

			if (tableVersion is null)
			{
				tableVersion = new TableVersion();
			}
			else
			{
				foreach ((string key, MembershipEntry membershipEntry) in tableVersion.Entries.ToArray())
				{
					if (string.IsNullOrEmpty(membershipEntry.ServiceId)
						|| string.IsNullOrEmpty(membershipEntry.ClusterId))
					{
						tableVersion.Entries.Remove(key);
					}
				}
			}

			tableVersion.VersionEtag = stateGetResponse.ETag;
			return tableVersion;
		}

		private async Task<string> Set(TableVersion tableVersion)
		{
			byte[] serialized = tableVersion.Serialize();
			string eTag = await _keyValueStore
				.Set(_membershipKey, serialized, tableVersion.VersionEtag)
				.ConfigureAwait(false);
			return eTag;
		}

		private static void Print(TableVersion tableVersion)
		{
			Console.Out.WriteLine();
			Console.Out.WriteLine("TableVersion: {0}", tableVersion.Version);
			foreach ((string key, MembershipEntry value) in tableVersion.Entries)
			{
				Console.Out.WriteLine("\t{0} - {1}", key, value.Status);
			}
		}
	}
}
