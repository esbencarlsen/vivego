using System;
using System.Collections.Generic;
using System.Text;

using vivego.Orleans.KeyValueProvider.Membership;

using Xunit;

namespace vivego.Orleans.KeyValueProvider.Tests
{
	public sealed class TableVersionSerializerTests
	{
		[Fact]
		public void CanSerializeAndDeserialize()
		{
			TableVersion versionInfo = new()
			{
				Version = 123,
				VersionEtag = Guid.NewGuid().ToString(),
				Entries =
				{
					{
						"A", new MembershipEntry
						{
							Status = "status",
							ClusterId = "clusterId",
							FaultZone = 123,
							HostName = "hostName",
							ProxyPort = 123,
							RoleName = "roleName",
							ServiceId = "serviceId",
							SiloName = "siloName",
							StartTime = DateTimeOffset.UtcNow,
							SuspectTimes = "suspectTimes",
							UpdateZone = 123,
							AmAliveTime = DateTimeOffset.UtcNow
						}
					},
					{"B", new MembershipEntry
						{
							Status = "status",
							ClusterId = "clusterId",
							FaultZone = 123,
							HostName = "hostName",
							ProxyPort = 123,
							RoleName = "roleName",
							ServiceId = "serviceId",
							SiloName = "siloName",
							StartTime = DateTimeOffset.UtcNow,
							SuspectTimes = "suspectTimes",
							UpdateZone = 123,
							AmAliveTime = DateTimeOffset.UtcNow
						}
					},
					{"C", new MembershipEntry
						{
							Status = "status",
							ClusterId = "clusterId",
							FaultZone = 123,
							HostName = "hostName",
							ProxyPort = 123,
							RoleName = "roleName",
							ServiceId = "serviceId",
							SiloName = "siloName",
							StartTime = DateTimeOffset.UtcNow,
							SuspectTimes = "suspectTimes",
							UpdateZone = 123,
							AmAliveTime = DateTimeOffset.UtcNow
						}
					}
				}
			};
			byte[] serialized = versionInfo.Serialize();

			var y = Encoding.UTF8.GetString(serialized);

			Assert.NotNull(serialized);
			Assert.NotEmpty(serialized);

			TableVersion? deserializedVersionInfo = serialized.Deserialize();
			Assert.NotNull(deserializedVersionInfo);
			if (deserializedVersionInfo is not null)
			{
				Assert.Equal(versionInfo.Version, deserializedVersionInfo.Version);
				Assert.Equal(versionInfo.VersionEtag, deserializedVersionInfo.VersionEtag);
				Assert.Equal(versionInfo.Entries.Count, deserializedVersionInfo.Entries.Count);
				foreach (KeyValuePair<string,MembershipEntry> pair in versionInfo.Entries)
				{
					bool exists = versionInfo.Entries.TryGetValue(pair.Key, out MembershipEntry? value);
					Assert.True(exists);
					if (exists && value is not null)
					{
						Assert.Equal(pair.Value.Status, value.Status);
						Assert.Equal(pair.Value.ClusterId, value.ClusterId);
						Assert.Equal(pair.Value.FaultZone, value.FaultZone);
						Assert.Equal(pair.Value.HostName, value.HostName);
						Assert.Equal(pair.Value.ProxyPort, value.ProxyPort);
						Assert.Equal(pair.Value.RoleName, value.RoleName);
						Assert.Equal(pair.Value.ServiceId, value.ServiceId);
						Assert.Equal(pair.Value.SiloName, value.SiloName);
						Assert.Equal(pair.Value.StartTime, value.StartTime);
						Assert.Equal(pair.Value.SuspectTimes, value.SuspectTimes);
						Assert.Equal(pair.Value.UpdateZone, value.UpdateZone);
						Assert.Equal(pair.Value.AmAliveTime, value.AmAliveTime);
					}
				}
			}
		}
	}
}
