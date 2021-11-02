using Microsoft.Extensions.DependencyInjection;

namespace vivego.ServiceBuilder.Abstractions
{
	public interface IServiceBuilder
	{
		string Name { get; }
		IServiceCollection Services { get; }
		IServiceBuilder Map<T>() where T : class;
		IServiceBuilder DependsOn<T>();
		IServiceBuilder DependsOnNamedService<TNamedService>(string? dependencyName = default) where TNamedService : class, INamedService;
	}
}