using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using vivego.core;
using vivego.Serializer.Model;

using Xunit;

namespace vivego.Serializer.Tests
{
	public sealed class SystemJsonSerializerTestEntity
	{
		public string? S { get; set; }
	}

	public sealed class SystemJsonSerializerTests
	{
		private static ISerializer GetSerializer()
		{
			ServiceCollection serviceCollection = new();
			serviceCollection.AddLogging();
			serviceCollection.AddSystemJsonSerializer();
			return serviceCollection
				.BuildServiceProvider(false)
				.GetRequiredService<ISerializer>();
		}

		[Fact]
		public async Task CanSerializeAndDeserialize()
		{
			ISerializer serializer = GetSerializer();

			SystemJsonSerializerTestEntity entity = new()
			{
				S = Guid.NewGuid().ToString()
			};

			SerializedValue serializedValue = await serializer.Serialize(entity).ConfigureAwait(false);

			Assert.NotEmpty(serializedValue.Data);
			Assert.Equal(
				serializedValue.Data[SerializerConstants.DataTypeName].ToStringUtf8(),
				new ParsedAssemblyQualifiedName(typeof(SystemJsonSerializerTestEntity).FullName!).TypeName);

			SystemJsonSerializerTestEntity? serializerTestEntity = await serializer
				.Deserialize<SystemJsonSerializerTestEntity>(serializedValue)
				.ConfigureAwait(false);

			Assert.NotNull(serializerTestEntity);
			Assert.Equal(entity.S, serializerTestEntity!.S);
		}
	}
}
