using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using vivego.Microsoft.Faster.PersistentLog;

using Xunit;

namespace vivego.Microsoft.Faster.Tests
{
	public sealed class FasterPersistentLogTests : IAsyncLifetime
	{
		private readonly IHost _host;
		private readonly DirectoryInfo _logFileDirectory;

		public FasterPersistentLogTests()
		{
			_logFileDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "deleteme"));
			string logFile = _logFileDirectory.FullName + "\\log";
			IHostBuilder hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(collection => collection.AddFasterPersistentLog(new FileInfo(logFile)));
			_host = hostBuilder.Build();
		}

		public Task InitializeAsync()
		{
			return _host.StartAsync();
		}

		public async Task DisposeAsync()
		{
			await _host.StopAsync().ConfigureAwait(false);
			_host.Dispose();
		}

		[Fact]
		public async Task LogFilesAreDeletedOnDispose()
		{
			FasterPersistentLog log = _host.Services.GetRequiredService<FasterPersistentLog>();
			await log.Write(new byte[10]).ConfigureAwait(false);
			Assert.True(Directory.Exists(_logFileDirectory.FullName));
			await log.DisposeAsync().ConfigureAwait(false);
			Assert.False(Directory.Exists(_logFileDirectory.FullName));
		}

		[Fact]
		public async Task CanSubscribeAfterPublish()
		{
			FasterPersistentLog log = _host.Services.GetRequiredService<FasterPersistentLog>();
			await log.Write(new byte[10]).ConfigureAwait(false);
			byte[][] all = await log.Subscribe().Take(1).ToArrayAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanSubscribeBeforePublish()
		{
			FasterPersistentLog log = _host.Services.GetRequiredService<FasterPersistentLog>();
			ValueTask<byte[][]> subscribeTask = log.Subscribe().Take(1).ToArrayAsync();
			await log.Write(new byte[10]).ConfigureAwait(false);
			byte[][] all = await subscribeTask.ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Single(all);
		}

		[Fact]
		public async Task CanSubscribeSequential()
		{
			FasterPersistentLog log = _host.Services.GetRequiredService<FasterPersistentLog>();
			await log.Write(new byte[10]).ConfigureAwait(false);
			byte[][] all1 = await log.Subscribe().Take(1).ToArrayAsync().ConfigureAwait(false);

			await log.Write(new byte[10]).ConfigureAwait(false);
			byte[][] all2 = await log.Subscribe().Take(1).ToArrayAsync().ConfigureAwait(false);

			Assert.NotNull(all1);
			Assert.Single(all1);

			Assert.NotNull(all2);
			Assert.Single(all2);
		}

		[Fact]
		public async Task SubscribeReturnsTailElementsOnFlush()
		{
			FasterPersistentLog log = _host.Services.GetRequiredService<FasterPersistentLog>();
			await log.Write(new byte[10]).ConfigureAwait(false);
			await log.Write(new byte[10]).ConfigureAwait(false);
			byte[][] all = await log.Subscribe()
				.SelectAwait(async b =>
				{
					await Task.Delay(300).ConfigureAwait(false);
					return b;
				})
				.Take(2)
				.ToArrayAsync()
				.ConfigureAwait(false);
			await _host.StopAsync().ConfigureAwait(false);
			Assert.NotNull(all);
			Assert.Equal(2, all.Length);
		}
	}
}
