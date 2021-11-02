using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Logging;

namespace vivego.core.Actors
{
	public sealed class ActorManager : ActorManager<Actor>
	{
		public ActorManager(ILogger logger) : base(name => new Actor(name, logger))
		{
		}
	}

	public class ActorManager<T> where T : Actor
	{
		private readonly Func<string, T> _actorFactory;
		private readonly ConcurrentDictionary<string, T> _actorDb = new(StringComparer.Ordinal);
		private readonly ActionBlock<string> _managementActor;
		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
		private readonly long _cleanupInterval = (long)TimeSpan.FromMinutes(1).TotalMilliseconds;
		private readonly TaskCompletionSource<object> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ActorManager(Func<string, T> actorFactory, Func<T, Task>? destructor = default)
		{
			_actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));

			HashSet<string> touched = new(StringComparer.Ordinal);
			_managementActor = new ActionBlock<string>(
				async id =>
				{
					if (string.IsNullOrEmpty(id))
					{
						foreach ((string _, T actor) in _actorDb.ToArray())
						{
							actor.Complete();
						}

						await Task.WhenAll(_actorDb
								.Select(pair => pair.Value.Completion))
							.ConfigureAwait(false);

						_taskCompletionSource.TrySetResult(default!);

						return;
					}

					touched.Add(id);

					if (_stopwatch.ElapsedMilliseconds > _cleanupInterval)
					{
						_stopwatch.Restart();
						foreach ((string key, _) in _actorDb.ToArray())
						{
							if (!touched.Contains(key) && _actorDb.TryRemove(key, out T? actor))
							{
								actor.Complete();
								if (destructor is not null)
								{
									await destructor(actor).ConfigureAwait(false);
								}
							}
						}

						touched.Clear();
					}
				}, new ExecutionDataflowBlockOptions()
				{
					EnsureOrdered = true,
					MaxDegreeOfParallelism = 1
				});
		}

		public T GetActor(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException("Value cannot be null or empty.", nameof(id));

			T? actor;
			while (!_actorDb.TryGetValue(id, out actor))
			{
				actor = _actorFactory(id);
				if (!_actorDb.TryAdd(id, actor))
				{
					actor.Complete();
				}
			}

			_managementActor.Post(id);
			return actor;
		}

		public void Complete()
		{
			_managementActor.Post(string.Empty);
		}

		public Task Completion => _taskCompletionSource.Task;
	}
}
