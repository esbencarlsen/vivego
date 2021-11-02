using System;

using Grpc.Net.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using vivego.KeyValue.Grpc;
using vivego.KeyValue.Tests.Helpers;
using vivego.Serializer;

using Xunit.Abstractions;

namespace vivego.KeyValue.Tests
{
#pragma warning disable CA1822 // Mark members as static
	public sealed class GrpcClientKeyValueStoreTests : KeyValueStoreTests
	{
		private readonly ITestOutputHelper _outputHelper;

		public GrpcClientKeyValueStoreTests(ITestOutputHelper outputHelper)
		{
			_outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
		}

		protected override void ConfigureServices(IServiceCollection serviceCollection)
		{
#pragma warning disable CA2000 // Dispose objects before losing scope
			GrpcTestFixture<Startup> grpcTestFixture = new();
#pragma warning restore CA2000 // Dispose objects before losing scope
			grpcTestFixture.LoggedMessage += (level, name, id, message, exception) =>
			{
				_outputHelper.WriteLine(message);
			};
			GrpcChannel channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
			{
				LoggerFactory = new NullLoggerFactory(),
				HttpHandler = grpcTestFixture.Handler
			});

			serviceCollection.AddMemoryCache();
			serviceCollection.AddGrpcClientKeyValueStore("UnitTest", channel);
		}
	}

	public sealed class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMemoryCache();
			services.AddGrpc();
			services.AddNewtonSoftJsonSerializer();
			services.AddInMemoryKeyValueStore("Default");
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<GrpcServerKeyValueStore>();
			});
		}
	}
}
