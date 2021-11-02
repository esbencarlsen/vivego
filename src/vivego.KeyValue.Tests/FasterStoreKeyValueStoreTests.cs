using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using vivego.KeyValue.Delete;
using vivego.KeyValue.Set;
using vivego.ServiceBuilder.Abstractions;

using Xunit;

namespace vivego.KeyValue.Tests
{
	[Trait("Category", "IntegrationTest")]
	public sealed class FasterStoreKeyValueStoreTests : KeyValueStoreTests
	{
		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
			IServiceBuilder builder = serviceCollection
				.AddMemoryCache()
				.AddInMemoryKeyValueStore("UnitTest");
			builder
				.Services
				.AddSingleton<WriteDelayPipelineBehaviour>()
				.AddSingleton<IPipelineBehavior<SetRequest, string>>(sp => sp.GetRequiredService<WriteDelayPipelineBehaviour>())
				.AddSingleton<IPipelineBehavior<DeleteRequest, bool>>(sp => sp.GetRequiredService<WriteDelayPipelineBehaviour>());
			builder.AddFasterPipelineBehavior(1);
		}
	}

	public sealed class WriteDelayPipelineBehaviour :
		IPipelineBehavior<SetRequest, string>,
		IPipelineBehavior<DeleteRequest, bool>
	{
		public async Task<string> Handle(SetRequest request,
			CancellationToken cancellationToken,
			RequestHandlerDelegate<string> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			await Task.Yield();
			await Task.Delay(300, cancellationToken).ConfigureAwait(false);
			await Task.Yield();

			string result = await Task.Factory!.StartNew(() => next(), cancellationToken,
				TaskCreationOptions.RunContinuationsAsynchronously,
				TaskScheduler.Default).Unwrap().ConfigureAwait(false);

			await Task.Yield();
			await Task.Delay(300, cancellationToken).ConfigureAwait(false);
			await Task.Yield();
			return result;
		}

		public async Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<bool> next)
		{
			if (next is null) throw new ArgumentNullException(nameof(next));
			await Task.Yield();
			await Task.Delay(300, cancellationToken).ConfigureAwait(false);
			await Task.Yield();

			bool result = await Task.Factory!.StartNew(() => next(), cancellationToken,
				TaskCreationOptions.None,
				TaskScheduler.Default).Unwrap().ConfigureAwait(false);

			await Task.Yield();
			await Task.Delay(300, cancellationToken).ConfigureAwait(false);
			await Task.Yield();
			return result;
		}
	}
}
