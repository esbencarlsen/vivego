using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using DotNet.Globbing;

using Microsoft.Extensions.Caching.Memory;

using vivego.MessageBroker.Abstractions;

namespace vivego.MessageBroker.SubscriptionManager;

public sealed record SubscriptionRegistrationEntry(SubscriptionType Type, string Pattern);

public sealed record SubscriptionRegistration(Func<string, bool> Predicate, HashSet<SubscriptionRegistrationEntry> SubscriptionRegistrationEntries);

public sealed class SubscriptionRepository
{
	private readonly IMemoryCache _memoryCache;
	private readonly IDictionary<string, SubscriptionRegistration> _db = new Dictionary<string, SubscriptionRegistration>();

	public SubscriptionRepository(IMemoryCache memoryCache)
	{
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	}

	public bool Add(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (string.IsNullOrEmpty(subscriptionId)) throw new ArgumentException("Value cannot be null or empty.", nameof(subscriptionId));
		if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Value cannot be null or empty.", nameof(pattern));

		SubscriptionRegistrationEntry subscriptionRegistrationEntry = new(type, pattern);
		if (_db.TryGetValue(subscriptionId, out SubscriptionRegistration? subscriptionRegistration2))
		{
			if (subscriptionRegistration2.SubscriptionRegistrationEntries.Add(subscriptionRegistrationEntry))
			{
				_db[subscriptionId] = subscriptionRegistration2 with
				{
					Predicate = MakePredicate(subscriptionRegistration2.SubscriptionRegistrationEntries)
				};

				return true;
			}

			return false;
		}

		HashSet<SubscriptionRegistrationEntry> hashSet = new()
		{
			subscriptionRegistrationEntry
		};
		_db[subscriptionId] = new SubscriptionRegistration(MakePredicate(hashSet), hashSet);
		return true;
	}

	public bool Remove(string subscriptionId, SubscriptionType type, string pattern)
	{
		if (_db.TryGetValue(subscriptionId, out SubscriptionRegistration? subscriptionRegistration))
		{
			SubscriptionRegistrationEntry subscriptionRegistrationEntry = new(type, pattern);
			if (subscriptionRegistration.SubscriptionRegistrationEntries.Remove(subscriptionRegistrationEntry))
			{
				if (subscriptionRegistration.SubscriptionRegistrationEntries.Count == 0)
				{
					_db.Remove(subscriptionId);
				}
				else
				{
					_db[subscriptionId] = subscriptionRegistration with
					{
						Predicate = MakePredicate(subscriptionRegistration.SubscriptionRegistrationEntries)
					};
				}

				return true;
			}
		}

		return false;
	}

	public IEnumerable<string> GetSubscriptionsFromTopic(string topic)
	{
		if (string.IsNullOrEmpty(topic)) throw new ArgumentException("Value cannot be null or empty.", nameof(topic));
		foreach ((string? key, SubscriptionRegistration? value) in _db)
		{
			if (value.Predicate(topic))
			{
				yield return key;
			}
		}
	}

	public IDictionary<string, SubscriptionRegistration> Get()
	{
		return _db;
	}

	private Func<string, bool> MakePredicate(HashSet<SubscriptionRegistrationEntry> hashSet)
	{
		List<Predicate<string>> predicates = new();
		foreach (SubscriptionRegistrationEntry subscriptionRegistration in hashSet)
		{
			switch (subscriptionRegistration.Type)
			{
				case SubscriptionType.Glob:
					predicates.Add(topic => GlobPredicate(topic, subscriptionRegistration.Pattern));
					break;
				case SubscriptionType.RegEx:
					predicates.Add(topic => RegexPredicate(topic, subscriptionRegistration.Pattern));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		return AllPredicates;

		bool AllPredicates(string topic)
		{
			return predicates.Any(predicate => predicate(topic));
		}

		bool RegexPredicate(string topic, string regexPattern)
		{
			Regex regex = _memoryCache.GetOrCreate(regexPattern, _ =>
			{
				_.SlidingExpiration = TimeSpan.FromMinutes(10);
				return new Regex(regexPattern, RegexOptions.IgnoreCase
					| RegexOptions.Singleline
					| RegexOptions.CultureInvariant
					| RegexOptions.IgnorePatternWhitespace
					| RegexOptions.Compiled, TimeSpan.FromSeconds(1));
			});

			return regex.IsMatch(topic);
		}

		bool GlobPredicate(string topic, string globPattern)
		{
			Glob glob = _memoryCache.GetOrCreate(globPattern, _ =>
			{
				_.SlidingExpiration = TimeSpan.FromMinutes(10);
				return Glob.Parse(globPattern);
			});

			return glob.IsMatch(topic);
		}
	}
}
