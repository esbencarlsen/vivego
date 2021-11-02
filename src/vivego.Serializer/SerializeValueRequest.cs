using System;

using MediatR;

using vivego.Serializer.Model;

namespace vivego.Serializer
{
	public sealed record SerializeValueRequest : IRequest<SerializedValue>
	{
		public object? Value { get; }

		public SerializeValueRequest(object? value)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}
	}
}