using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Proto;

namespace vivego.ProtoActor;

public sealed class SelfTerminatingActorContextDecorator : ActorContextDecorator
{
	private readonly Stopwatch _idleTime = Stopwatch.StartNew();
	private readonly TimeSpan _maxIdleTime;

	public SelfTerminatingActorContextDecorator(IContext context, TimeSpan maxIdleTime) : base(context)
	{
		_maxIdleTime = maxIdleTime;
	}

	public override Task Receive(MessageEnvelope envelope)
	{
		if (envelope is null) throw new ArgumentNullException(nameof(envelope));
		switch (envelope.Message)
		{
			case IdleCheckMessage:
				if (_idleTime.Elapsed > _maxIdleTime)
				{
					System.Root.Stop(Self);
				}

				return Task.CompletedTask;
		}

		_idleTime.Restart();
		return base.Receive(envelope);
	}
}
