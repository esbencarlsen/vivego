using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Orleans;
using Orleans.Runtime;

using vivego.core;

namespace vivego.Orleans
{
	public static class GrainFactoryExtensionsLifecycleParticipantExtensions
	{
		public static IServiceCollection RegisterOrleansLifecycleParticipantMonitor(this IServiceCollection serviceCollection)
		{
			if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

			if (serviceCollection.All(d => d.ServiceType != typeof(OrleansLifecycleParticipantMonitor)))
			{
				serviceCollection.AddSingleton<OrleansLifecycleParticipantMonitor>();
				serviceCollection.AddSingleton<IOrleansLifecycleParticipantMonitor>(sp => sp.GetRequiredService<OrleansLifecycleParticipantMonitor>());
				serviceCollection.AddSingleton<ISiloStatusListener>(sp => sp.GetRequiredService<OrleansLifecycleParticipantMonitor>());
				serviceCollection.AddSingleton<ILifecycleParticipant<IClusterClientLifecycle>>(sp => sp.GetRequiredService<OrleansLifecycleParticipantMonitor>());
				serviceCollection.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(sp => sp.GetRequiredService<OrleansLifecycleParticipantMonitor>());
			}

			return serviceCollection;
		}
	}

	public interface IOrleansLifecycleParticipantMonitor
	{
		Task SiloStatusChanged { get; }
		CancellationToken StoppingToken { get; }
	}

	internal class OrleansLifecycleParticipantMonitor : DisposableBase,
		IOrleansLifecycleParticipantMonitor,
		ISiloStatusListener,
		ILifecycleParticipant<IClusterClientLifecycle>,
		ILifecycleParticipant<ISiloLifecycle>
	{
		private TaskCompletionSource<object> _completionSource = new();
		private const int ClientOptionLoggerLifeCycleRing = int.MinValue;
		private readonly CancellationTokenSource _cancellationTokenSource = new();

		public Task SiloStatusChanged => _completionSource.Task;
		public CancellationToken StoppingToken => _cancellationTokenSource.Token;

		public OrleansLifecycleParticipantMonitor()
		{
			RegisterDisposable(_cancellationTokenSource);
		}

		public void SiloStatusChangeNotification(SiloAddress updatedSilo, SiloStatus status)
		{
			TaskCompletionSource<object> previousCompletionSource = _completionSource;
			_completionSource = new TaskCompletionSource<object>();
			previousCompletionSource.TrySetResult(default!);
		}

		public void Participate(IClusterClientLifecycle lifecycle)
		{
			lifecycle.Subscribe<OrleansLifecycleParticipantMonitor>(ClientOptionLoggerLifeCycleRing, OnStart, OnStop);
		}

		public void Participate(ISiloLifecycle lifecycle)
		{
			lifecycle.Subscribe<OrleansLifecycleParticipantMonitor>(ClientOptionLoggerLifeCycleRing, OnStart, OnStop);
		}

		private static Task OnStart(CancellationToken token)
		{
			return Task.CompletedTask;
		}

		private Task OnStop(CancellationToken token)
		{
			_cancellationTokenSource.Cancel(false);
			return Task.CompletedTask;
		}
	}
}