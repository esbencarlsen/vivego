using System;
using System.Threading;
using System.Threading.Tasks;

using Proto;

namespace vivego.ProtoActor;

public sealed class PeriodicMessageActorContextDecorator : ActorContextDecorator
{
	private readonly object _message;
	private readonly TimeSpan _sendEvery;

	public PeriodicMessageActorContextDecorator(IContext context,
		object message,
		TimeSpan sendEvery) : base(context)
	{
		_message = message;
		_sendEvery = sendEvery;
	}

	public override Task Receive(MessageEnvelope envelope)
	{
		if (envelope is null) throw new ArgumentNullException(nameof(envelope));
		switch (envelope.Message)
		{
			case Started:
				_ = SendRepeatedly();
				break;
		}

		return base.Receive(envelope);
	}

	private async Task SendRepeatedly()
	{
		using PeriodicTimer periodicTimer = new(_sendEvery);
		while (await WaitForNextTickAsync(periodicTimer).ConfigureAwait(false))
		{
			System.Root.Send(Self!, _message);
		}

		async Task<bool> WaitForNextTickAsync(PeriodicTimer timer)
		{
			try
			{
				return await timer.WaitForNextTickAsync(CancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Ignore
			}

			return false;
		}
	}
}
