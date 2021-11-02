using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace vivego.Orleans.KeyValueProvider.Membership
{
	public static class TableVersionSerializer
	{
		private static readonly JsonSerializerOptions s_serializeOptions = new();

		static TableVersionSerializer()
		{
			s_serializeOptions.Converters.Add(new DictionaryMembershipEntryConverter());
		}

		public static byte[] Serialize(this TableVersion tableVersion)
		{
			if (tableVersion is null) throw new ArgumentNullException(nameof(tableVersion));
			return JsonSerializer.SerializeToUtf8Bytes(tableVersion, s_serializeOptions);
		}

		public static TableVersion? Deserialize(this byte[] data)
		{
			if (data is null) throw new ArgumentNullException(nameof(data));
			if (data.Length == 0) return default;

			return JsonSerializer.Deserialize<TableVersion>(data, s_serializeOptions);
		}
	}

	public sealed class DictionaryMembershipEntryConverter : JsonConverter<IDictionary<string, MembershipEntry>>
	{
		public override IDictionary<string, MembershipEntry> Read(ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			if (typeToConvert is null) throw new ArgumentNullException(nameof(typeToConvert));
			if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

			IDictionary<string, MembershipEntry> value = new Dictionary<string, MembershipEntry>(StringComparer.Ordinal);
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject) return value;
				string? keyString = reader.GetString();
				if (keyString is null) break;
				reader.Read();
				MembershipEntry? membershipEntry = JsonSerializer.Deserialize<MembershipEntry>(ref reader, options);
				if (membershipEntry is null) break;
				value.Add(keyString, membershipEntry);
			}

			throw new JsonException("Error Occured");
		}

		public override void Write(Utf8JsonWriter writer, IDictionary<string, MembershipEntry> value,
			JsonSerializerOptions options)
		{
			if (writer is null) throw new ArgumentNullException(nameof(writer));
			if (value is null) throw new ArgumentNullException(nameof(value));
			writer.WriteStartObject();
			foreach (var (key, membershipEntry) in value)
			{
				writer.WritePropertyName(key);
				JsonSerializer.Serialize(writer, membershipEntry, typeof(MembershipEntry), options);
			}

			writer.WriteEndObject();
		}
	}
}
