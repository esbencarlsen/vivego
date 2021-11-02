using System;
using System.Text.Json.Serialization;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public sealed record MembershipEntry
	{
		[JsonPropertyName("serviceId")]
		public string ServiceId { get; set; } = string.Empty;

		[JsonPropertyName("clusterId")]
		public string ClusterId { get; set; } = string.Empty;

		[JsonPropertyName("status")]
		public string Status { get; set; } = string.Empty;

		/// <summary>
		/// The list of silos that suspect this silo. Managed by the Membership Protocol.
		/// </summary>
		[JsonPropertyName("suspectTimes")]
		public string SuspectTimes { get; set; } = string.Empty;

		/// <summary>Silo to clients TCP port. Set on silo startup.</summary>
		[JsonPropertyName("proxyPort")]
		public int ProxyPort { get; set; }

		/// <summary>
		/// The DNS host name of the silo. Equals to Dns.GetHostName(). Set on silo startup.
		/// </summary>
		[JsonPropertyName("hostName")]
		public string HostName { get; set; } = string.Empty;

		[JsonPropertyName("siloName")]
		public string SiloName { get; set; } = string.Empty;

		[JsonPropertyName("roleName")]
		public string RoleName { get; set; } = string.Empty;

		[JsonPropertyName("updateZone")]
		public int UpdateZone { get; set; }

		[JsonPropertyName("faultZone")]
		public int FaultZone { get; set; }

		/// <summary>
		/// Time this silo was started. For diagnostics and troubleshooting only.
		/// </summary>
		[JsonPropertyName("startTime")]
		public DateTimeOffset StartTime { get; set; }

		/// <summary>
		/// the last time this silo reported that it is alive. For diagnostics and troubleshooting only.
		/// </summary>
		// ReSharper disable once InconsistentNaming
		[JsonPropertyName("amAliveTime")]
		public DateTimeOffset AmAliveTime { get; set; }

		public string ETag { get; set; } = string.Empty;
	}
}