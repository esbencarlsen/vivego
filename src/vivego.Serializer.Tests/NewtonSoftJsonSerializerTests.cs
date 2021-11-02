using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using vivego.Serializer.Model;

using Xunit;

namespace vivego.Serializer.Tests
{
	public sealed class NewtonSoftJsonSerializerTests
	{
		private static ISerializer GetSerializer()
		{
			ServiceCollection serviceCollection = new();
			serviceCollection.AddLogging();
			serviceCollection.AddNewtonSoftJsonSerializer();
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

			SystemJsonSerializerTestEntity? serializerTestEntity = await serializer
				.Deserialize<SystemJsonSerializerTestEntity>(serializedValue)
				.ConfigureAwait(false);

			Assert.NotNull(serializerTestEntity);
			Assert.Equal(entity.S, serializerTestEntity!.S);
		}

		[Fact]
		public async Task CanSerializeAndDeserializeJObject()
		{
			ISerializer serializer = GetSerializer();

			object? entity = JsonConvert.DeserializeObject(@"{
    ""A"":1
}");

			SerializedValue serializedValue = await serializer.Serialize(entity!).ConfigureAwait(false);

			Assert.NotEmpty(serializedValue.Data);

			object? serializerTestEntity = await serializer
				.Deserialize(serializedValue)
				.ConfigureAwait(false);

			Assert.NotNull(serializerTestEntity);
		}
	}
}
