using System;
using System.Threading.Tasks;

using Proto;

namespace vivego.ProtoActor;

public readonly record struct ScheduleMessage(PID Target, object Message, TimeSpan SendMessageIn);
public readonly record struct SendMessage(PID Target, object Message);

public sealed class ThrottledMessageActorContextDecorator : ActorContextDecorator
{
	private bool _timerRunning;

	public ThrottledMessageActorContextDecorator(IContext context) : base(context)
	{
	}

	public override Task Receive(MessageEnvelope envelope)
	{
		if (envelope is null) throw new ArgumentNullException(nameof(envelope));
		switch (envelope.Message)
		{
			case ScheduleMessage scheduleMessage:
				if (!_timerRunning)
				{
					_timerRunning = true;
					_ = ScheduleSend(scheduleMessage);
				}

				return Task.CompletedTask;
			case SendMessage(var target, var message):
				_timerRunning = false;
				Send(target, message);
				return Task.CompletedTask;
		}

		return base.Receive(envelope);
	}

	private async Task ScheduleSend(ScheduleMessage scheduleMessage)
	{
		await Task.Delay(scheduleMessage.SendMessageIn).ConfigureAwait(false);
		System.Root.Send(Self!, new SendMessage(scheduleMessage.Target, scheduleMessage.Message));
	}
}

public static class ContextExtensions
{
	public static void SendIn(this IContext context,
		object message,
		TimeSpan sendMessageIn)
	{
		if (context is null) throw new ArgumentNullException(nameof(context));
		context.Send(context.Self, new ScheduleMessage(context.Self, message, sendMessageIn));
	}

	public static void SendIn(this IContext context,
		PID target,
		object message,
		TimeSpan sendMessageIn)
	{
		if (context is null) throw new ArgumentNullException(nameof(context));
		context.Send(context.Self, new ScheduleMessage(target, message, sendMessageIn));
	}
}
