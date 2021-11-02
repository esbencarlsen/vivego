using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Orleans.Runtime;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public static class ConvertHelper
	{
		public static MembershipEntry ToOrleansMembershipEntry(this global::Orleans.MembershipEntry orleansMembershipEntry, 
			string serviceId,
			string clusterid,
			string etag)
		{
			if (orleansMembershipEntry is null) throw new ArgumentNullException(nameof(orleansMembershipEntry));
			if (serviceId is null) throw new ArgumentNullException(nameof(serviceId));
			if (etag is null) throw new ArgumentNullException(nameof(etag));
			string serializedSuspectTimes = JsonConvert.SerializeObject(orleansMembershipEntry
				.SuspectTimes
				.Select(tuple => (tuple.Item1.ToParsableString(), tuple.Item2)));
			return new MembershipEntry
			{
				ServiceId = serviceId,
				Status = orleansMembershipEntry.Status.ToString(),
				SuspectTimes = serializedSuspectTimes,
				ProxyPort = orleansMembershipEntry.ProxyPort,
				HostName = orleansMembershipEntry.HostName,
				SiloName = orleansMembershipEntry.SiloName,
				RoleName = orleansMembershipEntry.RoleName,
				UpdateZone = orleansMembershipEntry.UpdateZone,
				FaultZone = orleansMembershipEntry.FaultZone,
				StartTime = orleansMembershipEntry.StartTime,
				AmAliveTime = orleansMembershipEntry.IAmAliveTime,
				ClusterId = clusterid,
				ETag = etag
			};
		}

		public static global::Orleans.MembershipEntry ToOrleansMembershipEntry(this MembershipEntry membershipEntry, string id)
		{
			if (membershipEntry is null) throw new ArgumentNullException(nameof(membershipEntry));
			(string, DateTime)[] suspectTimes = JsonConvert
				.DeserializeObject<ValueTuple<string, DateTime>[]>(membershipEntry.SuspectTimes) ?? Array.Empty<(string, DateTime)>();
			List<Tuple<SiloAddress, DateTime>> deserializedSuspectTimes = suspectTimes
				.Select(tuple => new Tuple<SiloAddress, DateTime>(SiloAddress.FromParsableString(tuple.Item1), tuple.Item2))
				.ToList();
			return new global::Orleans.MembershipEntry
			{
				SiloAddress = SiloAddress.FromParsableString(id),
				Status = Enum.TryParse(membershipEntry.Status, true, out SiloStatus status) ? status : SiloStatus.None,
				SuspectTimes = deserializedSuspectTimes,
				ProxyPort = membershipEntry.ProxyPort,
				HostName = membershipEntry.HostName,
				SiloName = membershipEntry.SiloName,
				RoleName = membershipEntry.RoleName,
				UpdateZone = membershipEntry.UpdateZone,
				FaultZone = membershipEntry.FaultZone,
				StartTime = membershipEntry.StartTime.UtcDateTime,
				IAmAliveTime = membershipEntry.AmAliveTime.UtcDateTime
			};
		}

		public static global::Orleans.TableVersion ToOrleansTableVersion(this TableVersion tableVersion)
		{
			if (tableVersion is null) throw new ArgumentNullException(nameof(tableVersion));
			return new global::Orleans.TableVersion(tableVersion.Version, tableVersion.VersionEtag);
		}
	}
}