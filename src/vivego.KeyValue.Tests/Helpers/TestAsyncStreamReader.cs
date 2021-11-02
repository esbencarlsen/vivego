using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Grpc.Core;

using Channel = System.Threading.Channels.Channel;

namespace vivego.KeyValue.Tests.Helpers
{
	public sealed class TestAsyncStreamReader<T> : IAsyncStreamReader<T> where T : class
	{
		private readonly Channel<T> _channel;
		private readonly ServerCallContext _serverCallContext;

		public TestAsyncStreamReader(ServerCallContext serverCallContext)
		{
			_channel = Channel.CreateUnbounded<T>();
			_serverCallContext = serverCallContext ?? throw new ArgumentNullException(nameof(serverCallContext));
		}

		public T Current { get; private set; } = null!;

		public async Task<bool> MoveNext(CancellationToken cancellationToken)
		{
			_serverCallContext.CancellationToken.ThrowIfCancellationRequested();

			if (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
			{
				if (_channel.Reader.TryRead(out T? message))
				{
					Current = message;
					return true;
				}
			}

			Current = null!;
			return false;
		}

		public void AddMessage(T message)
		{
			if (!_channel.Writer.TryWrite(message))
			{
				throw new InvalidOperationException("Unable to write message.");
			}
		}

		public void Complete()
		{
			_channel.Writer.Complete();
		}
	}
}
