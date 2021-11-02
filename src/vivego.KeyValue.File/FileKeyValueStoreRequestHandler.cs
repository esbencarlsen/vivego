using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using MediatR;

using Microsoft.Extensions.Logging;

using vivego.core;
using vivego.KeyValue.Abstractions.Model;
using vivego.KeyValue.Clear;
using vivego.KeyValue.Delete;
using vivego.KeyValue.Features;
using vivego.KeyValue.Get;
using vivego.KeyValue.Set;

namespace vivego.KeyValue.File
{
	public sealed class FileKeyValueStoreRequestHandler : IKeyValueStoreRequestHandler
	{
		private readonly string _path;
		private readonly ILogger<FileKeyValueStoreRequestHandler> _logger;
		private readonly Task<KeyValueStoreFeatures> _features;

		public FileKeyValueStoreRequestHandler(string path,
			ILogger<FileKeyValueStoreRequestHandler> logger)
		{
			_path = path ?? throw new ArgumentNullException(nameof(path));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			DirectoryInfo directoryInfo = new(_path);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}

			_features = Task.FromResult(new KeyValueStoreFeatures
			{
				SupportsTtl = false,
				SupportsEtag = false,
				MaximumDataSize = int.MaxValue,
				MaximumKeyLength = 248 - _path.Length
			});
		}

		public Task<KeyValueStoreFeatures> Handle(FeaturesRequest request, CancellationToken cancellationToken)
		{
			return _features;
		}

		public async Task<string> Handle(SetRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));
			string fileName = Path.Combine(_path, MakeValidFileName(request.Entry.Key));
			KeyValueEntry keyValueEntry = request.Entry.ConvertToKeyValueEntry();
			await System.IO.File
				.WriteAllBytesAsync(fileName, keyValueEntry.ToByteArray(), cancellationToken)
				.ConfigureAwait(false);
			return keyValueEntry.ETag;
		}

		public async Task<KeyValueEntry> Handle(GetRequest request, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.Key)) throw new ArgumentException("Value cannot be null or empty.", nameof(request.Key));
			string fileName = Path.Combine(_path, MakeValidFileName(request.Key));
			if (!System.IO.File.Exists(fileName))
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

#pragma warning disable CA2000 // Dispose objects before losing scope
			FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Inheritable, 1024);
#pragma warning restore CA2000 // Dispose objects before losing scope
			await using ConfiguredAsyncDisposable _ = fileStream.ConfigureAwait(false);
			KeyValueEntry keyValueEntry = KeyValueEntry.Parser.ParseFrom(fileStream);

			if (keyValueEntry.ExpiresAtUnixTimeSeconds > 0
				&& DateTimeOffset.FromUnixTimeSeconds(keyValueEntry.ExpiresAtUnixTimeSeconds) < DateTimeOffset.UtcNow)
			{
				return KeyValueEntryExtensions.KeyValueNull;
			}

			return keyValueEntry;
		}

		public Task<bool> Handle(DeleteRequest request, CancellationToken cancellationToken)
		{
			if (request.Entry is null) throw new ArgumentNullException(nameof(request.Entry));
			if (string.IsNullOrEmpty(request.Entry.Key)) throw new ArgumentException(nameof(request.Entry.Key));
			string fileName = Path.Combine(_path, MakeValidFileName(request.Entry.Key));
			return Task.FromResult(DeleteFile(fileName));
		}

		public Task<Unit> Handle(ClearRequest request, CancellationToken cancellationToken)
		{
			new DirectoryInfo(_path).Delete(true);
			try { new DirectoryInfo(_path).Create(); }
			catch
			{
				// ignored
			}

			return Unit.Task;
		}

		private bool DeleteFile(string fileName)
		{
			if (!System.IO.File.Exists(fileName))
			{
				return false;
			}

			try
			{
				System.IO.File.Delete(fileName);
				return true;
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error while deleting file: {FileName}", fileName);
			}

			return false;
		}

		private static string MakeValidFileName(string input)
		{
			return SanitizedFileName.Sanitize(input);
		}
	}
}
