using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using Newtonsoft.Json;

using vivego.Serializer.Model;

namespace vivego.Serializer.NewtonJsonSerializer
{
	public sealed class NewtonJsonSerializerRequestHandler :
		IRequestHandler<SerializeValueRequest, SerializedValue>,
		IRequestHandler<DeSerializeValueRequest, object?>
	{
		private readonly JsonSerializer _serializer;

		public NewtonJsonSerializerRequestHandler(JsonSerializerSettings jsonSerializerSettings)
		{
			_serializer = JsonSerializer.Create(jsonSerializerSettings);
		}

		public Task<SerializedValue> Handle(SerializeValueRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));

			using JsonWriter jsonWriter = new JsonTextWriter(new StringWriter());
			using StringWriter stringWriter = new(new StringBuilder(256), CultureInfo.InvariantCulture);
			using JsonTextWriter jsonTextWriter = new(stringWriter);
			_serializer.Serialize(jsonTextWriter, request.Value);
			string serializedString = stringWriter.ToString();
			ByteString byteString = ByteString.CopyFromUtf8(serializedString);
			SerializedValue serializedValue = new();
			serializedValue.Data[SerializerConstants.DataName] = byteString;
			return Task.FromResult(serializedValue);
		}

		public Task<object?> Handle(DeSerializeValueRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.SerializedValue.Data.TryGetValue(SerializerConstants.DataName, out ByteString byteString))
			{
				string serializedString = byteString.ToString(Encoding.UTF8);
				using StringReader stringReader = new(serializedString);
				using JsonTextReader jsonTextReader = new(stringReader);
				object? value = _serializer.Deserialize(jsonTextReader, request.Type);
				return Task.FromResult(value);
			}

			throw new SerializationException($"Could not deserialize from {nameof(NewtonJsonSerializerRequestHandler)} because serialized value is missing");
		}
	}
}
