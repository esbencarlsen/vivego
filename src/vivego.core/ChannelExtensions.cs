using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace vivego.core
{
	public static class ChannelExtensions
	{
		public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Channel<T> channel,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) where T : notnull
		{
			if (channel is null) throw new ArgumentNullException(nameof(channel));
			await using ConfiguredAsyncDisposable _ = cancellationToken.Register(state =>
			{
				if (state is Channel<T> innerChannel)
				{
					innerChannel.Writer.TryComplete();
				}
			}, channel).ConfigureAwait(false);
			while (await channel.Reader.WaitToReadAsync(CancellationToken.None).ConfigureAwait(false))
			{
				while (channel.Reader.TryRead(out T? value))
				{
					yield return value;
				}
			}
		}
	}
}
