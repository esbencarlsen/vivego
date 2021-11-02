using MediatR;

namespace vivego.ServiceInvocation.Invocation
{
	public readonly record struct ServiceInvocationRequest(string GroupId, ServiceInvocationEntry Entry) : IRequest<ServiceInvocationEntryResponse>;
}
