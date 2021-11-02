using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.Collection.EventStore;
using vivego.core;
using vivego.KeyValue;
using vivego.Serializer;

using Xunit;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.Tests.EventStore
{
	public sealed class DefaultTypedEventStoreTests : TypedEventStoreTests
	{
		protected override IEventStore<T> MakeEventStore<T>()
		{
			IHost host = new HostBuilder()
				.ConfigureServices(collection =>
				{
					collection.AddInMemoryKeyValueStore("Default");
					collection.AddEventStore();
					collection.AddSystemJsonSerializer();
					collection.AddLogging();
					collection.AddMemoryCache();
					collection.AddSystemJsonSerializer();
				})
				.Build();
			host.Start();
			RegisterDisposable(() =>
			{
				using (host)
				{
					host.StopAsync();
				}
			});

			return new DefaultTypedEventStore<T>(host.Services.GetRequiredService<IEventStore>(),
				host.Services.GetRequiredService<ISerializer>());
		}
	}

	public sealed class TypedEventStoreTestEntity
	{
	}

	public abstract class TypedEventStoreTests : DisposableBase
	{
		protected abstract IEventStore<T> MakeEventStore<T>() where T : notnull;

		protected virtual TypedEventStoreTestEntity GetTypedEventStoreTestEntity()
		{
			return new();
		}

		[Fact]
		public virtual async Task CanAppend()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			Version version = await eventStore.Append(id,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(0, version.Begin);
			Assert.Equal(0, version.End);
		}

		[Fact]
		public virtual async Task CanAppendSequential()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			Version version = await eventStore.Append(id,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version.Begin);
			Assert.Equal(1, version.End);
		}

		[Fact]
		public virtual async Task ThrowExceptionIfStreamNotExist()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await Assert.ThrowsAsync<WrongExpectedVersionException>(() => eventStore.Append(id,
					ExpectedVersion.StreamExists,
					GetTypedEventStoreTestEntity()))
				.ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task CanAppendOnlyIfStreamAlreadyExists()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			Version version = await eventStore.Append(id,
				ExpectedVersion.StreamExists,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version.Begin);
			Assert.Equal(1, version.End);
		}

		[Fact]
		public virtual async Task CanAppendOnlyIfStreamDoesNotExist()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.NoStream,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task ThrowExceptionNoStreamExist()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			await Assert.ThrowsAsync<WrongExpectedVersionException>(() => eventStore.Append(id,
				ExpectedVersion.NoStream,
				GetTypedEventStoreTestEntity())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendNullKey()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			await Assert.ThrowsAsync<ArgumentException>(() => eventStore.Append(null!,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendEmptyKey()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			await Assert.ThrowsAsync<ArgumentException>(() => eventStore.Append(string.Empty,
				ExpectedVersion.Any,
				GetTypedEventStoreTestEntity())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendNullEventData()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await Assert.ThrowsAsync<ArgumentNullException>(() => eventStore.Append(id,
				ExpectedVersion.Any,
				null!)).ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task CanGetEmpty()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public virtual async Task CanGetDeleted()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any, GetTypedEventStoreTestEntity()).ConfigureAwait(false);
			await eventStore.Delete(id).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public virtual async Task CanGetMany()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity())).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(100, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 100)));
		}

		[Fact]
		public virtual async Task CanGetManyFrom()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity())).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore
				.GetFrom(id, 10)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(10, 90)));
		}

		[Fact]
		public virtual async Task CanGetManyFromBehind()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity())).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore
				.GetTo(id, 89)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 90)));
		}

		[Fact]
		public virtual async Task CanGetManyReverse()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity())).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore.GetAllReverse(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(100, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 100).Reverse()));
		}

		[Fact]
		public virtual async Task CanGetManyFromReverse()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
					Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity()))
				.ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore
				.GetFromReverse(id, 10)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(10, 90).Reverse()));
		}

		[Fact]
		public virtual async Task CanGetManyFromBehindReverse()
		{
			IEventStore<TypedEventStoreTestEntity> eventStore = MakeEventStore<TypedEventStoreTestEntity>();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => GetTypedEventStoreTestEntity())).ConfigureAwait(false);
			IRecordedEvent<TypedEventStoreTestEntity>[] all = await eventStore
				.GetToReverse(id, 89)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 90).Reverse()));
		}
	}
}
