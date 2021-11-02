using System;

using MediatR;

using vivego.Serializer.Model;

namespace vivego.Serializer
{
	public sealed record DeSerializeValueRequest : IRequest<object?>
	{
		public SerializedValue SerializedValue { get; }
		public Type? Type { get; }

		public DeSerializeValueRequest(
			SerializedValue serializedValue,
			Type? type)
		{
			SerializedValue = serializedValue ?? throw new ArgumentNullException(nameof(serializedValue));
			Type = type;
		}
	}
}