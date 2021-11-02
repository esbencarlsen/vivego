using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using vivego.Serializer.Model;

namespace vivego.Serializer
{
	public sealed class DefaultSerializer : ISerializer
	{
		public string Name { get; }
		private readonly IMediator _mediator;

		public DefaultSerializer(string name, IMediator mediator)
		{
			Name = name;
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		}

		public Task<SerializedValue> Serialize<T>(T value, CancellationToken cancellationToken = default) =>
			_mediator.Send(new SerializeValueRequest(value), cancellationToken);

		public async Task<T?> Deserialize<T>(SerializedValue serializedValue, CancellationToken cancellationToken = default) where T : notnull
		{
			object? value = await _mediator
				.Send(new DeSerializeValueRequest(serializedValue, typeof(T)), cancellationToken)
				.ConfigureAwait(false);
			if (value is T t)
			{
				return t;
			}

			throw new SerializationException($"Expected deserialized type to be {typeof(T).FullName} but found {value?.GetType().FullName ?? "(null)"}");
		}

		public async Task<object?> Deserialize(SerializedValue serializedValue, CancellationToken cancellationToken = default)
		{
			object? value = await _mediator
				.Send(new DeSerializeValueRequest(serializedValue, null), cancellationToken)
				.ConfigureAwait(false);
			return value;
		}
	}
}
