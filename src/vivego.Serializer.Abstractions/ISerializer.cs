using System.Threading;
using System.Threading.Tasks;

using vivego.Serializer.Model;
using vivego.ServiceBuilder.Abstractions;

namespace vivego.Serializer
{
	public interface ISerializer : INamedService
	{
		Task<SerializedValue> Serialize<T>(T value, CancellationToken cancellationToken = default);
		Task<T?> Deserialize<T>(SerializedValue serializedValue, CancellationToken cancellationToken = default) where T : notnull;
		Task<object?> Deserialize(SerializedValue serializedValue, CancellationToken cancellationToken = default);
	}
}