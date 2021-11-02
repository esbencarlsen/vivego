using System;

using FluentValidation;

namespace vivego.ServiceInvocation
{
	public sealed class DefaultServiceInvocationOptionsValidator : AbstractValidator<DefaultServiceInvocationOptions>
	{
		public DefaultServiceInvocationOptionsValidator()
		{
			RuleFor(options => options.DefaultRequestTimeout).ExclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(1));
			RuleFor(options => options.DefaultResponseTimeout).ExclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(1));
			RuleFor(options => options.HttpLogRetension).ExclusiveBetween(TimeSpan.Zero, TimeSpan.MaxValue);
			RuleFor(options => options.HttpRequestHeaderPrefix).MinimumLength(0).MaximumLength(255);
			RuleForEach(options => options.DefaultRetryDelays).ExclusiveBetween(TimeSpan.Zero, TimeSpan.MaxValue);
		}
	}
}
