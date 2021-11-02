using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Grpc.Core;

using Channel = System.Threading.Channels.Channel;

namespace vivego.KeyValue.Tests.Helpers
{
	public sealed class TestServerStreamWriter<T> : IServerStreamWriter<T> where T : class
	{
		private readonly Channel<T> _channel;
		private readonly ServerCallContext _serverCallContext;

		public TestServerStreamWriter(ServerCallContext serverCallContext)
		{
			_serverCallContext = serverCallContext ?? throw new ArgumentNullException(nameof(serverCallContext));
			_channel = Channel.CreateUnbounded<T>();
		}

		public WriteOptions? WriteOptions { get; set; }

		public Task WriteAsync(T message)
		{
			if (_serverCallContext.CancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled(_serverCallContext.CancellationToken);
			}

			if (!_channel.Writer.TryWrite(message)) throw new InvalidOperationException("Unable to write message.");

			return Task.CompletedTask;
		}

		public void Complete()
		{
			_channel.Writer.Complete();
		}

		public IAsyncEnumerable<T> ReadAllAsync() => _channel.Reader.ReadAllAsync();

		public async Task<T?> ReadNextAsync()
		{
			if (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
			{
				if (_channel.Reader.TryRead(out T? message))
				{
					return message;
				}
			}

			return null;
		}
	}
}
