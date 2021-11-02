using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using vivego.core;
using vivego.Serializer.Model;

namespace vivego.Serializer.SystemJsonSerializer
{
	public sealed class SystemJsonSerializerRequestHandler :
		IRequestHandler<SerializeValueRequest, SerializedValue>,
		IRequestHandler<DeSerializeValueRequest, object?>
	{
		private readonly JsonSerializerOptions _jsonSerializerOptions;
		private readonly TypeNameHelper _typeNameHelper;

		public SystemJsonSerializerRequestHandler(
			TypeNameHelper typeNameHelper,
			JsonSerializerOptions jsonSerializerOptions)
		{
			_typeNameHelper = typeNameHelper ?? throw new ArgumentNullException(nameof(typeNameHelper));
			_jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
		}

		public Task<SerializedValue> Handle(SerializeValueRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			string serializedString = JsonSerializer.Serialize(request.Value, _jsonSerializerOptions);
			ByteString byteString = ByteString.CopyFromUtf8(serializedString);
			SerializedValue serializedValue = new();
			serializedValue.Data[SerializerConstants.DataName] = byteString;

			if (request.Value is not null)
			{
				string typeName = _typeNameHelper.GetTypeName(request.Value.GetType());
				serializedValue.Data[SerializerConstants.DataTypeName] = ByteString.CopyFromUtf8(typeName);
			}

			return Task.FromResult(serializedValue);
		}

		public Task<object?> Handle(DeSerializeValueRequest request, CancellationToken cancellationToken)
		{
			if (request is null) throw new ArgumentNullException(nameof(request));
			if (request.SerializedValue.Data.TryGetValue(SerializerConstants.DataName, out ByteString byteString))
			{
				string serializedString = byteString.ToString(Encoding.UTF8);

				if (request.Type is not null && !request.Type.IsInterface)
				{
					object? requestTypedValue = JsonSerializer.Deserialize(serializedString, request.Type, _jsonSerializerOptions);
					return Task.FromResult(requestTypedValue);
				}

				if (request.SerializedValue.Data.TryGetValue(SerializerConstants.DataTypeName, out ByteString? typeNameByteString)
					&& typeNameByteString is not null)
				{
					string? returnTypeNameString = typeNameByteString.ToStringUtf8();
					Type? returnType = _typeNameHelper.GetTypeFromName(returnTypeNameString);
					if (returnType is not null)
					{
						object? returnTypedValue = JsonSerializer.Deserialize(serializedString, returnType, _jsonSerializerOptions);
						return Task.FromResult(returnTypedValue);
					}
				}

				object? objectTypedValue = JsonSerializer.Deserialize<object>(serializedString, _jsonSerializerOptions);
				return Task.FromResult(objectTypedValue);
			}

			throw new SerializationException($"Could not deserialize from {nameof(SystemJsonSerializerRequestHandler)} because serialized value is missing");
		}
	}
}
