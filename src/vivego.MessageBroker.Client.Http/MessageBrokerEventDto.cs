using System.Text.Json;
using System.Text.Json.Serialization;

namespace vivego.MessageBroker.Client.Http;

[Serializable]
public sealed record MessageBrokerEventDto
{
	[JsonPropertyName("eventId")]
	public long EventId { get; init; }

	[JsonPropertyName("createdAt")]
	public long CreatedAt { get; init; }

	[JsonExtensionData]
	public Dictionary<string, JsonElement> ExtensionData { get; init; } = new();
}
