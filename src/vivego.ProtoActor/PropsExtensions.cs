using System;

using Proto;

namespace vivego.ProtoActor;

public static class PropsExtensions
{
	public static Props WithIdleSelfTermination(this Props props,
		TimeSpan maxIdleTime,
		TimeSpan checkEvery)
	{
		if (props is null) throw new ArgumentNullException(nameof(props));
		return props
			.WithPeriodicMessage(checkEvery, IdleCheckMessage.Instance)
			.WithContextDecorator(context => new SelfTerminatingActorContextDecorator(context, maxIdleTime));
	}

	public static Props WithPeriodicMessage(this Props props,
		TimeSpan sendEvery,
		object message)
	{
		if (props is null) throw new ArgumentNullException(nameof(props));
		return props
			.WithContextDecorator(context => new PeriodicMessageActorContextDecorator(context, message, sendEvery));
	}

	public static Props WithThrottledMessage(this Props props)
	{
		if (props is null) throw new ArgumentNullException(nameof(props));
		return props
			.WithContextDecorator(context => new ThrottledMessageActorContextDecorator(context));
	}
}
