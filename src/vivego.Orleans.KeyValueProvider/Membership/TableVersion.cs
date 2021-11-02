using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public sealed class TableVersion
	{
		/// <summary>
		/// The version part of this TableVersion. Monotonically increasing number.
		/// </summary>
		[JsonPropertyName("version")]
		public int Version { get; set; }

		/// <summary>
		/// The etag of this TableVersion, used for validation of table update operations.
		/// </summary>
		[JsonPropertyName("versionEtag")]
		public string VersionEtag { get; set; } = string.Empty;

		#pragma warning disable CA2227
		[JsonPropertyName("entries")]
		[JsonConverter(typeof(DictionaryMembershipEntryConverter))]
		public IDictionary<string, MembershipEntry> Entries { get; set; } = new Dictionary<string, MembershipEntry>(StringComparer.Ordinal);
	}
}
