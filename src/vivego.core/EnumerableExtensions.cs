using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace vivego.core
{
	public static class EnumerableExtensions
	{
		public static Task ForEachAsync<TSource>(
			this IEnumerable<TSource> items,
			Func<TSource, Task> action,
			int maxDegreesOfParallelism)
		{
			ActionBlock<TSource> actionBlock = new(action, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreesOfParallelism
			});

			foreach (TSource item in items.EmptyIfNull())
			{
				actionBlock.Post(item);
			}

			actionBlock.Complete();
			return actionBlock.Completion;
		}

		public static async Task<IEnumerable<TResult>> Select<TSource, TResult>(
			this IEnumerable<TSource> items,
			Func<TSource, Task<TResult>> action,
			int maxDegreesOfParallelism,
			CancellationToken cancellationToken = default)
		{
			TransformBlock<TSource, TResult> transformBlock = new(action, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = maxDegreesOfParallelism,
				CancellationToken = cancellationToken,
				EnsureOrdered = true
			});

			BufferBlock<TResult> bufferBlock = new(new DataflowBlockOptions
			{
				CancellationToken = cancellationToken,
				EnsureOrdered = true
			});
			using IDisposable _ = transformBlock.LinkTo(bufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
			foreach (TSource item in items.EmptyIfNull())
			{
				transformBlock.Post(item);
			}

			transformBlock.Complete();
			await transformBlock.Completion.ConfigureAwait(false);

			if (bufferBlock.TryReceiveAll(out IList<TResult>? result))
			{
				return result;
			}

			return Array.Empty<TResult>();
		}

		public static async Task<T[]> FromAsyncArray<T>(this IEnumerable<Task<T>> source)
		{
			Task<T>[] actualizedTasks = source.EmptyIfNull().ToArray();
			await Task.WhenAll(actualizedTasks).ConfigureAwait(false);
			return actualizedTasks
				.Select(task => task.Result)
				.ToArray();
		}

		public static Task FromAsyncArray(this IEnumerable<Task> source)
		{
			return Task.WhenAll(source.EmptyIfNull());
		}

		public static Task FromAsyncArray(this IEnumerable<ValueTask> source)
		{
			return Task.WhenAll(source.EmptyIfNull().Select(valueTask => valueTask.AsTask()));
		}

		public static Task WhenAll(this IEnumerable<Task> source)
		{
			return Task.WhenAll(source.EmptyIfNull());
		}

		public static Task WhenAny(this IEnumerable<Task> source)
		{
			return Task.WhenAny(source.EmptyIfNull());
		}

		/// <summary>
		///     Adds one or more sequences to the begin of the current sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="enums">Sequences to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToBegin<T>(this IEnumerable<T> target, params IEnumerable<T>[] enums)
		{
			foreach (IEnumerable<T> sequence in enums.EmptyIfNull())
			{
				foreach (T item in sequence.EmptyIfNull())
				{
					yield return item!;
				}
			}

			foreach (T item in target.EmptyIfNull())
			{
				yield return item!;
			}
		}

		/// <summary>
		///     Adds one or more elements to the begin of sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="values">Elements to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToBegin<T>(this IEnumerable<T> target, params T[] values)
		{
			foreach (T value in values.EmptyIfNull())
			{
				yield return value!;
			}

			foreach (T value in target.EmptyIfNull())
			{
				yield return value!;
			}
		}

		/// <summary>
		///     Adds one or more sequences to the end of the current sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="enums">Sequences to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToEnd<T>(this IEnumerable<T> target, params IEnumerable<T>[] enums)
		{
			foreach (T item in target.EmptyIfNull())
			{
				yield return item!;
			}

			foreach (IEnumerable<T> sequence in enums.EmptyIfNull())
			{
				foreach (T item in sequence.EmptyIfNull())
				{
					yield return item!;
				}
			}
		}

		/// <summary>
		///     Adds one or more elements to sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="values">Elements to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToEnd<T>(this IEnumerable<T> target, params T[] values)
		{
			foreach (T item in target.EmptyIfNull())
			{
				yield return item!;
			}

			foreach (T value in values.EmptyIfNull())
			{
				yield return value!;
			}
		}

		public static T? Coalesce<T>(this IEnumerable<T> enumerable) where T : class
		{
			return enumerable
				.EmptyIfNull()
				.FirstOrDefault();
		}

		public static IEnumerable<T> Delete<T>(this IEnumerable<T> target, int position, int length)
		{
			int pos = 0;
			foreach (T item in target.EmptyIfNull())
			{
				if (pos == position && length > 0)
				{
					length--;
					continue;
				}

				pos++;
				yield return item!;
			}
		}

		public static IEnumerable<T> Delete<T>(this IEnumerable<T> target, T element)
		{
			return target
				.EmptyIfNull()
				.Where(item => item?.Equals(element) ?? false);
		}

		/// <summary>
		///     If sequence is null then an empty list is returned, otherwise the sequence.
		/// </summary>
		/// <param name="sequence"></param>
		/// <returns></returns>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? sequence)
		{
			return sequence ?? Enumerable.Empty<T>();
		}

		/// <summary>
		///     If sequence is null then an empty list is returned, otherwise the sequence.
		/// </summary>
		/// <param name="sequence"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<T> EmptyIfNull<T>(this IAsyncEnumerable<T>? sequence)
		{
			return sequence ?? AsyncEnumerable.Empty<T>();
		}

		/// <summary>
		///     Determines whether the enumerable contains elements that match the conditions defined by the specified predicate
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="enumerable">Target enumeration</param>
		/// <param name="predicate">Condition of the element to search for</param>
		/// <returns>true, if specified element is existed, otherwise, false</returns>
		/// <exception cref="System.ArgumentNullException">One of the input arguments is null</exception>
		public static bool Exists<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
		{
			return enumerable
				.EmptyIfNull()
				.Any(item => predicate(item));
		}

		/// <summary>
		///     Iterates through all sequence and performs specified action on each
		///     element
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="enumerable">Target enumeration</param>
		/// <param name="action">Action</param>
		/// <exception cref="System.ArgumentNullException">One of the input arguments is null</exception>
		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			if (action is null) throw new ArgumentNullException(nameof(action));
			foreach (T elem in enumerable.EmptyIfNull())
			{
				action(elem);
			}
		}

		public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
		{
			if (enumerable is null)
			{
				return true;
			}

			if (enumerable is IList list)
			{
				if (list.Count == 0)
				{
					return true;
				}
			}
			else if (!enumerable.Any())
			{
				return true;
			}

			return false;
		}

		public static string Join<T>(this IEnumerable<T> target, string separator) where T : notnull
		{
			return string.Join(separator, target.Select(i => i.ToString()).ToArray());
		}

		public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T>? source, int pageSize)
		{
			using IEnumerator<T> enumerator = source?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Take(pageSize);
			}
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : notnull
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
			foreach (T? t in enumerable)
			{
				if (t is not null)
				{
					yield return t;
				}
			}
		}

		public static async IAsyncEnumerable<T> WhereNotNull<T>(this IAsyncEnumerable<T?> asyncEnumerable) where T : notnull
		{
			if (asyncEnumerable == null) throw new ArgumentNullException(nameof(asyncEnumerable));
			await foreach (T? notNull in asyncEnumerable.ConfigureAwait(false))
			{
				if (notNull is not null)
				{
					yield return notNull;
				}
			}
		}

		private static IEnumerable<T> Take<T>(this IEnumerator<T> source, int pageSize)
		{
			if (source is null) throw new ArgumentNullException(nameof(source));

			do
			{
				pageSize--;
				yield return source.Current!;
			} while (pageSize > 0 && source.MoveNext());
		}

		/// <summary>
		///     Returns all distinct elements of the given source, where "distinctness"
		///     is determined via a projection and the default equality comparer for the projected type.
		/// </summary>
		/// <remarks>
		///     This operator uses deferred execution and streams the results, although
		///     a set of already-seen keys is retained. If a key is seen multiple times,
		///     only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <returns>
		///     A sequence consisting of distinct elements from the source sequence,
		///     comparing them by the specified key projection.
		/// </returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector)
		{
			return source
				.EmptyIfNull()
				.DistinctBy(keySelector, EqualityComparer<TKey>.Default);
		}

		/// <summary>
		///     Returns all distinct elements of the given source, where "distinctness"
		///     is determined via a projection and the specified comparer for the projected type.
		/// </summary>
		/// <remarks>
		///     This operator uses deferred execution and streams the results, although
		///     a set of already-seen keys is retained. If a key is seen multiple times,
		///     only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <param name="comparer">
		///     The equality comparer to use to determine whether or not keys are equal.
		///     If null, the default equality comparer for <c>TSource</c> is used.
		/// </param>
		/// <returns>
		///     A sequence consisting of distinct elements from the source sequence,
		///     comparing them by the specified key projection.
		/// </returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer)
		{
			if (keySelector is null)
			{
				throw new ArgumentNullException(nameof(keySelector));
			}

			return DistinctByImpl(source, keySelector, comparer);
		}

		private static IEnumerable<TSource> DistinctByImpl<TSource, TKey>(IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer)
		{
			HashSet<TKey> knownKeys = new(comparer);
			return source
				.EmptyIfNull()
				.Where(element => knownKeys.Add(keySelector(element)));
		}

		public static IEnumerable<TSource> ExceptBy<TSource>(this IEnumerable<TSource> source,
			Predicate<TSource> keySelector,
			IEqualityComparer<TSource>? equalityComparer = null)
		{
			IEnumerable<TSource> sourceArray = source as TSource[] ?? source.ToArray();
			return sourceArray
				.EmptyIfNull()
				.Except(sourceArray
					.EmptyIfNull()
					.Where(s => keySelector(s)), equalityComparer);
		}

		public static IEnumerable<IList<T>> ForEachBatch<T>(this IEnumerable<T> enumerable, int batchSize)
		{
			List<T> batch = new(batchSize);
			foreach (T t in enumerable.EmptyIfNull())
			{
				batch.Add(t);
				if (batch.Count >= batchSize)
				{
					yield return batch;
					batch.Clear();
				}
			}

			if (batch.Count > 0)
			{
				yield return batch;
			}
		}
	}
}
