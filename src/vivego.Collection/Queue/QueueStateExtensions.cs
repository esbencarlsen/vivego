using System;

using vivego.Queue.Model;

namespace vivego.Collection.Queue
{
	internal static class QueueStateExtensions
	{
		public static long Count(this QueueState queueState)
		{
			if (queueState is null) throw new ArgumentNullException(nameof(queueState));
			return queueState.Tail - queueState.Head;
		}
	}
}