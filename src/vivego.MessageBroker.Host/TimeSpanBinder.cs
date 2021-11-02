using System;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using TimeSpanParserUtil;

namespace vivego.MessageBroker.Host;

public sealed class TimeSpanBinder : IModelBinder
{
	private readonly TimeSpanParserOptions _timeSpanParserOptions = new()
	{
		ColonedDefault = Units.Days,
		FormatProvider = CultureInfo.InvariantCulture
	};

	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		ArgumentNullException.ThrowIfNull(bindingContext);

		string modelName = bindingContext.ModelName;
		ValueProviderResult values = bindingContext.ValueProvider.GetValue(modelName);
		if (values.Length == 0)
		{
			return Task.CompletedTask;
		}

		if (TimeSpanParser.TryParse(values.FirstValue, _timeSpanParserOptions, out TimeSpan timeSpan))
		{
			bindingContext.Result = ModelBindingResult.Success(timeSpan);
		}

		return Task.CompletedTask;
	}
}
