using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace vivego.core
{
	public sealed class AsyncQueue<T> : IAsyncEnumerable<T> where T : notnull
	{
		private readonly object _lockObject = new();
		private readonly List<Channel<T>> _channelsOut = new();

		public bool HasSubscribers => _channelsOut.Count > 0;

		public Task WriteAsync(T item)
		{
			return Task.WhenAll(_channelsOut
				.Select(channel => channel.Writer.WriteAsync(item).AsTask()));
		}

		public void TryWrite(T item)
		{
			foreach (Channel<T> channel in _channelsOut)
			{
				channel.Writer.TryWrite(item);
			}
		}

		private IDisposable Subscribe(Channel<T> channel)
		{
			lock (_lockObject)
			{
				_channelsOut.Add(channel);
			}

			IDisposable disposable = new DisposableLambda(() =>
			{
				lock (_lockObject)
				{
					_channelsOut.Remove(channel);
				}
			});

			return disposable;
		}

#pragma warning disable 8424
		public async IAsyncEnumerator<T> GetAsyncEnumerator([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore 8424
		{
			Channel<T> channelOut = Channel.CreateUnbounded<T>();
			using IDisposable _ = Subscribe(channelOut);
			await foreach (T value in channelOut.ToAsyncEnumerable(cancellationToken).ConfigureAwait(false))
			{
				yield return value;
			}
		}
	}

	internal sealed class DisposableLambda : DisposableBase
	{
		public DisposableLambda(Action action)
		{
			RegisterDisposable(action);
		}
	}
}
