using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.ServiceBuilder
{
	public sealed class NamedServiceManager<T> : IServiceManager<T> where T : class, INamedService
	{
		private readonly IServiceProvider _serviceProvider;
		private IDictionary<string, T>? _stores;

		public NamedServiceManager(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public T Get(string name)
		{
			_stores ??= _serviceProvider
				.GetRequiredService<IEnumerable<T>>()
				.ToDictionary(keyValueStore => keyValueStore.Name, keyValueStore => keyValueStore, StringComparer.Ordinal);
			if (_stores.TryGetValue(name, out T? namedService))
			{
				return namedService;
			}

			throw new ResolveException($"Service with name: {name} not registered");
		}

		public IEnumerable<T> GetAll()
		{
			_stores ??= _serviceProvider
				.GetRequiredService<IEnumerable<T>>()
				.ToDictionary(keyValueStore => keyValueStore.Name, keyValueStore => keyValueStore, StringComparer.Ordinal);
			return _stores.Select(pair => pair.Value);
		}
	}
}
