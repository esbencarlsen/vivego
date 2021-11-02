using System.Collections.Generic;

namespace vivego.ServiceBuilder.Abstractions
{
	public interface IServiceManager<out T> where T : INamedService
	{
		T Get(string name);
		IEnumerable<T> GetAll();
	}
}