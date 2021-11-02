using System;

using Microsoft.Extensions.Logging;

using vivego.core;

namespace vivego.KeyValue.Tests.Helpers
{
	internal class ForwardingLoggerProvider : ILoggerProvider
	{
		private readonly LogMessage _logAction;

		public ForwardingLoggerProvider(LogMessage logAction) => _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));

		public ILogger CreateLogger(string categoryName)
		{
			if (string.IsNullOrEmpty(categoryName)) throw new ArgumentException("Value cannot be null or empty.", nameof(categoryName));
			return new ForwardingLogger(categoryName, _logAction);
		}

		public void Dispose()
		{
		}

		internal class ForwardingLogger : ILogger
		{
			private readonly string _categoryName;
			private readonly LogMessage _logAction;

			public ForwardingLogger(string categoryName, LogMessage logAction)
			{
				_categoryName = categoryName;
				_logAction = logAction;
			}

			public IDisposable BeginScope<TState>(TState state) => new EmptyDisposable();

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			{
				_logAction(logLevel, _categoryName, eventId, formatter(state, exception), exception);
			}
		}
	}
}
