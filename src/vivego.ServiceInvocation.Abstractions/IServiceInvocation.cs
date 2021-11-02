using System.Threading;
using System.Threading.Tasks;

using vivego.ServiceBuilder.Abstractions;

namespace vivego.ServiceInvocation
{
	public interface IServiceInvocation : INamedService
	{
		Task Add(string groupId,
			ServiceInvocationEntry serviceInvocationEntry,
			CancellationToken cancellationToken = default);
	}
}