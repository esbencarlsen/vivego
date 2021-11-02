using System;
using System.Collections.Generic;
using System.Linq;

using MediatR;
using MediatR.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.ServiceBuilder
{
	public class DefaultServiceBuilder<TService> : IServiceBuilder where TService : class, INamedService
	{
		private readonly IServiceCollection _serviceCollection;
		private IServiceProvider? _serviceProvider;
		private readonly List<Type> _dependencies = new();

		public string Name { get; }
		public IServiceCollection Services { get; } = new ServiceCollection();
		public IServiceProvider ServiceProvider => _serviceProvider ??= Services.BuildServiceProvider();

		public DefaultServiceBuilder(
			string name,
			IServiceCollection serviceCollection)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			Name = name;
			_serviceCollection = serviceCollection ?? throw new ArgumentNullException(nameof(serviceCollection));

			Map<TService>();
			_serviceCollection.TryAddSingleton<IServiceManager<TService>, NamedServiceManager<TService>>();
			DependsOn<IServiceManager<TService>>();
			TryAddMediatR(Services);
		}

		protected static void TryAddMediatR(IServiceCollection serviceCollection)
		{
			if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));
			if (serviceCollection.All(d => d.ServiceType != typeof(Mediator)))
			{
				serviceCollection.AddSingleton<IMediator, Mediator>();
				serviceCollection.AddSingleton(p => new ServiceFactory(p.GetService!));
				serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
				serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
				serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionProcessorBehavior<,>));
				serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
			}
		}

		public IServiceBuilder DependsOn<T>()
		{
			lock (_dependencies)
			{
				_dependencies.Add(typeof(T));
			}

			return this;
		}

		public IServiceBuilder DependsOnNamedService<TNamedService>(string? dependencyName = default) where TNamedService : class, INamedService
		{
			if (string.IsNullOrEmpty(dependencyName))
			{
				DependsOn<TNamedService>();
			}
			else
			{
				DependsOn<IServiceManager<TNamedService>>();
				Services.AddSingleton(provider => provider.GetRequiredService<IServiceManager<TNamedService>>().Get(dependencyName));
			}

			return this;
		}

		public IServiceBuilder Map<T>() where T : class
		{
			_serviceCollection.AddTransient(sp =>
			{
				MapDependenciesToInnerServiceProvider(sp);
				return ServiceProvider.GetRequiredService<T>();
			});
			return this;
		}

		protected void MapDependenciesToInnerServiceProvider(IServiceProvider serviceProvider)
		{
			Type[] dependencies;
			lock (_dependencies)
			{
				dependencies = _dependencies.Distinct().ToArray();
				_dependencies.Clear();
			}

			if (_serviceProvider is not null && dependencies.Length > 0)
			{
				throw new Exception("Race condition");
			}

			foreach (Type type in dependencies)
			{
				Services.AddTransient(type, _ => serviceProvider.GetRequiredService(type));
			}
		}
	}
}
