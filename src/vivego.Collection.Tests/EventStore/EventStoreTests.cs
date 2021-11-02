using System;
using System.Linq;
using System.Threading.Tasks;

using vivego.Collection.EventStore;
using vivego.core;
using vivego.EventStore;

using Xunit;

using Version = vivego.EventStore.Version;

namespace vivego.Collection.Tests.EventStore
{
	public abstract class EventStoreTests : DisposableBase
	{
		protected abstract IEventStore MakeEventStore();

		[Fact]
		public virtual async Task CanAppend()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			Version version = await eventStore.Append(id,
				ExpectedVersion.Any,
				new EventData()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(0, version.Begin);
			Assert.Equal(0, version.End);
		}

		[Fact]
		public virtual async Task CanAppendSequential()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				new EventData()).ConfigureAwait(false);
			Version version = await eventStore.Append(id,
				ExpectedVersion.Any,
				new EventData()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version.Begin);
			Assert.Equal(1, version.End);
		}

		[Fact]
		public virtual async Task ThrowExceptionIfStreamNotExist()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await Assert.ThrowsAsync<WrongExpectedVersionException>(() => eventStore.Append(id,
				ExpectedVersion.StreamExists,
				new EventData())).ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task CanAppendOnlyIfStreamAlreadyExists()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				new EventData()).ConfigureAwait(false);
			Version version = await eventStore.Append(id,
				ExpectedVersion.StreamExists,
				new EventData()).ConfigureAwait(false);
			Assert.NotNull(version);
			Assert.Equal(1, version.Begin);
			Assert.Equal(1, version.End);
		}

		[Fact]
		public virtual async Task CanAppendOnlyIfStreamDoesNotExist()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.NoStream,
				new EventData()).ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task ThrowExceptionNoStreamExist()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id,
				ExpectedVersion.Any,
				new EventData()).ConfigureAwait(false);
			await Assert.ThrowsAsync<WrongExpectedVersionException>(() => eventStore.Append(id,
				ExpectedVersion.NoStream,
				new EventData())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendNullKey()
		{
			IEventStore eventStore = MakeEventStore();
			await Assert.ThrowsAsync<ArgumentException>(() => eventStore.Append(null!,
				ExpectedVersion.Any,
				new EventData())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendEmptyKey()
		{
			IEventStore eventStore = MakeEventStore();
			await Assert.ThrowsAsync<ArgumentException>(() => eventStore.Append(string.Empty,
				ExpectedVersion.Any,
				new EventData())).ConfigureAwait(false);
		}

		[Fact]
		public async Task CannotAppendNullEventData()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await Assert.ThrowsAsync<ArgumentNullException>(() => eventStore.Append(id,
				ExpectedVersion.Any,
				null!)).ConfigureAwait(false);
		}

		[Fact]
		public virtual async Task CanGetEmpty()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			RecordedEvent[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public virtual async Task CanGetDeleted()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any, new EventData()).ConfigureAwait(false);
			await eventStore.Delete(id).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Empty(all);
		}

		[Fact]
		public virtual async Task CanGetMany()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore.GetAll(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(100, all.Length);
			Assert.True(all.Select(recordedEvent => (int) recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 100)));
		}

		[Fact]
		public virtual async Task CanGetManyFrom()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore
				.GetFrom(id, 10)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int) recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(10, 90)));
		}

		[Fact]
		public virtual async Task CanGetManyFromBehind()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore
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
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore.GetAllReverse(id).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(100, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 100).Reverse()));
		}

		[Fact]
		public virtual async Task CanGetManyFromReverse()
		{
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore
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
			IEventStore eventStore = MakeEventStore();
			string id = Guid.NewGuid().ToString();
			await eventStore.Append(id, ExpectedVersion.Any,
				Enumerable.Range(0, 100).Select(_ => new EventData())).ConfigureAwait(false);
			RecordedEvent[] all = await eventStore
				.GetToReverse(id, 89)
				.ToArrayAsync()
				.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(90, all.Length);
			Assert.True(all.Select(recordedEvent => (int)recordedEvent.EventNumber).SequenceEqual(Enumerable.Range(0, 90).Reverse()));
		}
	}
}
