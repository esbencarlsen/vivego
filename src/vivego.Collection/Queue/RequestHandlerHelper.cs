using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using vivego.Queue.Model;

namespace vivego.Collection.Queue
{
	public static class RequestHandlerHelper
	{
		public static string MakeKey(string id, long version) => $"{id}_{version}";

		[return: NotNull]
		public static IEnumerable<long> Range(this QueueState queueState, long? skip = default)
		{
			if (queueState is null) throw new ArgumentNullException(nameof(queueState));
			for (long i = queueState.Head + skip.GetValueOrDefault(0); i < queueState.Tail; i++)
			{
				yield return i;
			}
		}
		
		[return: NotNull]
		public static IEnumerable<long> ReverseRange(this QueueState queueState, long? skip = default)
		{
			if (queueState is null) throw new ArgumentNullException(nameof(queueState));
			for (long i = queueState.Tail - 1 - skip.GetValueOrDefault(0); i >= queueState.Head; i--)
			{
				yield return i;
			}
		}
	}
}