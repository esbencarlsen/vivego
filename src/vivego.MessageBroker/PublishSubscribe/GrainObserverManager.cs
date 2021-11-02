using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Orleans.Runtime;

namespace vivego.MessageBroker.PublishSubscribe;

/// <summary>
/// Maintains a collection of grain observers.
/// </summary>
/// <typeparam name="T">
/// The grain observer type.
/// </typeparam>
public sealed class GrainObserverManager<T> : IEnumerable<T> where T : IAddressable
{
	/// <summary>
	/// The observers.
	/// </summary>
	private readonly Dictionary<T, Stopwatch> _observers = new();

	/// <summary>
	/// Gets or sets the expiration time span, after which observers are lazily removed.
	/// </summary>
	public TimeSpan ExpirationDuration { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Gets the number of observers.
	/// </summary>
	public int Count => _observers.Count;

	/// <summary>
	/// Removes all observers.
	/// </summary>
	public void Clear()
	{
		_observers.Clear();
	}

	/// <summary>
	/// Ensures that the provided <paramref name="observer"/> is subscribed, renewing its subscription.
	/// </summary>
	/// <param name="observer">The observer.</param>
	public void Subscribe(T observer)
	{
		// Add or update the subscription.
		_observers[observer] = Stopwatch.StartNew();
	}

	/// <summary>
	/// Ensures that the provided <paramref name="observer"/> is unsubscribed.
	/// </summary>
	/// <param name="observer">The observer.</param>
	public void Unsubscribe(T observer)
	{
		_observers.Remove(observer);
	}

	/// <summary>
	/// Notifies all observers.
	/// </summary>
	/// <param name="notification">
	/// The notification delegate to call on each observer.
	/// </param>
	/// <returns>
	/// A <see cref="Task"/> representing the work performed.
	/// </returns>
	public async Task Notify(Func<T, Task> notification)
	{
		if (notification is null) throw new ArgumentNullException(nameof(notification));

		List<T>? defunct = default;
		foreach ((T key, var value) in _observers)
		{
			if (value.Elapsed > ExpirationDuration)
			{
				// Expired observers will be removed.
				defunct ??= new List<T>();
				defunct.Add(key);
				continue;
			}

			try
			{
				await notification(key).ConfigureAwait(false);
			}
#pragma warning disable CA1031
			catch (Exception)
			{
				// Failing observers are considered defunct and will be removed..
				defunct ??= new List<T>();
				defunct.Add(key);
			}
		}

		// Remove defunct observers.
		if (defunct != default(List<T>))
		{
			foreach (T observer in defunct)
			{
				_observers.Remove(observer);
			}
		}
	}

	/// <summary>
	/// Notifies all observers />.
	/// </summary>
	/// <param name="notification">
	/// The notification delegate to call on each observer.
	/// </param>
	public IEnumerable<T> Notify(Action<T> notification)
	{
		if (notification is null) throw new ArgumentNullException(nameof(notification));

		List<T>? defunct = default;
		foreach ((T key, Stopwatch value) in _observers)
		{
			if (value.Elapsed > ExpirationDuration)
			{
				// Expired observers will be removed.
				defunct ??= new List<T>();
				defunct.Add(key);
				continue;
			}

			try
			{
				notification(key);
			}
			catch
			{
				// Failing observers are considered defunct and will be removed..
				defunct ??= new List<T>();
				defunct.Add(key);
			}
		}

		// Remove defunct observers.
		if (defunct is not null)
		{
			foreach (T observer in defunct)
			{
				_observers.Remove(observer);
				yield return observer;
			}
		}
	}

	/// <summary>
	/// Removed all expired observers.
	/// </summary>
	public void ClearExpired()
	{
		List<T>? defunct = default;
		foreach ((T key, Stopwatch stopwatch) in _observers)
		{
			if (stopwatch.Elapsed > ExpirationDuration)
			{
				// Expired observers will be removed.
				defunct ??= new List<T>();
				defunct.Add(key);
			}
		}

		// Remove defunct observers.
		if (defunct is not null)
		{
			foreach (T observer in defunct)
			{
				_observers.Remove(observer);
			}
		}
	}

	/// <summary>
	/// Returns the enumerator for all observers.
	/// </summary>
	/// <returns>The enumerator for all observers.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return _observers.Keys.GetEnumerator();
	}

	/// <summary>
	/// Returns the enumerator for all observers.
	/// </summary>
	/// <returns>The enumerator for all observers.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _observers.Keys.GetEnumerator();
	}
}
