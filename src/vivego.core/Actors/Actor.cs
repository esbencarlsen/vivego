using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Logging;

namespace vivego.core.Actors
{
	public class Actor
	{
		private readonly string _id;
		private readonly ILogger _logger;
		private readonly ActionBlock<Func<Task>> _actionBlock = new(
			func => func(), new ExecutionDataflowBlockOptions
			{
				EnsureOrdered = true,
				MaxDegreeOfParallelism = 1
			});

		public Actor(string id,
			ILogger logger)
		{
			_id = id ?? throw new ArgumentNullException(nameof(id));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public void Complete()
		{
			_actionBlock.Complete();
		}

		public void Post<T>(T state, Func<T, Task> func)
		{
			if (func is null) throw new ArgumentNullException(nameof(func));

			_actionBlock.Post(async () =>
			{
				try
				{
					Task task = func(state);
					if (task != null)
					{
						await task.ConfigureAwait(false);
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"Error while running in actor with id {_id}");
				}
			});
		}

		public void Post<T>(T state, Action<T> action)
		{
			if (action is null) throw new ArgumentNullException(nameof(action));

			_actionBlock.Post(() =>
			{
				try
				{
					action(state);
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"Error while running in actor with id {_id}");
				}

				return Task.CompletedTask;
			});
		}

		public Task Completion => _actionBlock.Completion;
	}
}
