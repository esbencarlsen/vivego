using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using vivego.core;
using vivego.MessageBroker.Abstractions;
using vivego.MessageBroker.SubscriptionManager;

using Xunit;

namespace vivego.MessageBroker.Tests;

public sealed class SubscriptionRepositoryTests : DisposableBase
{
	private readonly SubscriptionRepository _subscriptionRepository;

	public SubscriptionRepositoryTests()
	{
#pragma warning disable CA2000
		MemoryCache memoryCache = new(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		RegisterDisposable(memoryCache);
		_subscriptionRepository = new(memoryCache);
	}

	[Fact]
	public void CanGetEmpty()
	{
		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
	}

	[Fact]
	public void CanRemoveNonExisting()
	{
		bool removed = _subscriptionRepository.Remove("A", SubscriptionType.Glob, "*");
		Assert.False(removed);
	}

	[Fact]
	public void CanAddGlob()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
		Assert.Single(db);
		Assert.NotNull(db["A"]);
		Assert.Single(db["A"].SubscriptionRegistrationEntries);

		Assert.Single(_subscriptionRepository.GetSubscriptionsFromTopic("A"));
	}

	[Fact]
	public void CanAddSameGlobMultipleTimes()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);
		added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.False(added);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
		Assert.Single(db);
		Assert.NotNull(db["A"]);
		Assert.Single(db["A"].SubscriptionRegistrationEntries);

		Assert.Single(_subscriptionRepository.GetSubscriptionsFromTopic("A"));
	}

	[Fact]
	public void CanAddRegex()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
		Assert.Single(db);
		Assert.NotNull(db["A"]);
		Assert.Single(db["A"].SubscriptionRegistrationEntries);

		Assert.Single(_subscriptionRepository.GetSubscriptionsFromTopic("A"));
	}

	[Fact]
	public void CanAddRegexAndGlobToSameSubscription()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		added = _subscriptionRepository.Add("A", SubscriptionType.RegEx, "*");
		Assert.True(added);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
		Assert.Single(db);
		Assert.NotNull(db["A"]);
		Assert.Equal(2, db["A"].SubscriptionRegistrationEntries.Count);

		Assert.Single(_subscriptionRepository.GetSubscriptionsFromTopic("A"));
	}

	[Fact]
	public void CanAddAndRemove()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		bool removed = _subscriptionRepository.Remove("A", SubscriptionType.Glob, "*");
		Assert.True(removed);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.NotNull(db);
		Assert.Empty(db);
	}

	[Fact]
	public void CanAddMultipleAndRemove()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "**");
		Assert.True(added);

		bool removed = _subscriptionRepository.Remove("A", SubscriptionType.Glob, "*");
		Assert.True(removed);

		IDictionary<string, SubscriptionRegistration> db = _subscriptionRepository.Get();
		Assert.Single(db);
		Assert.NotNull(db["A"]);
		Assert.Single(db["A"].SubscriptionRegistrationEntries);

		Assert.Single(_subscriptionRepository.GetSubscriptionsFromTopic("A"));
	}

	[Fact]
	public void WillMatchMultipleSubscriptions()
	{
		bool added = _subscriptionRepository.Add("A", SubscriptionType.Glob, "*");
		Assert.True(added);

		added = _subscriptionRepository.Add("B", SubscriptionType.Glob, "*");
		Assert.True(added);

		added = _subscriptionRepository.Add("C", SubscriptionType.RegEx, ".*");
		Assert.True(added);

		Assert.Equal(3, _subscriptionRepository.GetSubscriptionsFromTopic("x").Count());
	}
}
