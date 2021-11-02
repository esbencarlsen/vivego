using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace vivego.core
{
	public static class AsyncEnumerableExtensions
	{
		public static async IAsyncEnumerable<IList<T>> ForEachBatch<T>(this IAsyncEnumerable<T> enumerable,
			int batchSize,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (enumerable is null) throw new ArgumentNullException(nameof(enumerable));
			List<T> batch = new (batchSize);
			await foreach (T t in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
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
		
		public static async IAsyncEnumerable<T> Unwrap<T>(this Task<IAsyncEnumerable<T>> asyncEnumerableTask,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			if (asyncEnumerableTask is null) throw new ArgumentNullException(nameof(asyncEnumerableTask));
			IAsyncEnumerable<T> asyncEnumerable = await asyncEnumerableTask.ConfigureAwait(false);
			await foreach (T t in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return t;
			}
		}
	}
}