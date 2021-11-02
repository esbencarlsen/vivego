using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable MA0025 // TODO Implement the functionality
namespace vivego.logger.web
{
	/// <summary>
	/// Works with logging
	/// Works with Request size limiting
	/// </summary>
	public sealed class ReadStreamSplitterStream : Stream
	{
		private readonly Stream _source;
		private readonly Stream[] _targets;

		public ReadStreamSplitterStream(
			Stream source,
			params Stream[] targets)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_targets = targets ?? throw new ArgumentNullException(nameof(targets));
			if (_targets.Length == 0) throw new ArgumentOutOfRangeException(nameof(targets));
		}

		public override void Flush()
		{
			_source.Flush();
			foreach (Stream target in _targets)
			{
				target.Flush();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int result = _source.Read(buffer, offset, count);
			foreach (Stream target in _targets)
			{
				target.Write(buffer.AsSpan(offset, result));
			}

			return result;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			foreach (Stream target in _targets)
			{
				if (target.CanSeek)
				{
					target.Seek(offset, origin);
				}
			}

			return _source.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotImplementedException();
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotImplementedException();
		}

		public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			await using MemoryStream copyStream = new();
			await using ConfiguredAsyncDisposable _ = copyStream.ConfigureAwait(false);
			await _source.CopyToAsync(copyStream, bufferSize, cancellationToken).ConfigureAwait(false);
			copyStream.Position = 0;
			await copyStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
			foreach (Stream target in _targets)
			{
				copyStream.Position = 0;
				await copyStream.CopyToAsync(target, bufferSize, cancellationToken).ConfigureAwait(false);
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotImplementedException();
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotImplementedException();
		}

		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			await _source.FlushAsync(cancellationToken).ConfigureAwait(false);
			foreach (Stream target in _targets)
			{
				await target
					.FlushAsync(cancellationToken)
					.ConfigureAwait(false);
			}
		}

		public override int Read(Span<byte> buffer)
		{
			int result = _source.Read(buffer);
			foreach (Stream target in _targets)
			{
				target.Write(buffer.ToArray(), 0, result);
			}

			return result;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
#pragma warning disable CA1835
			int result = await _source.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
			foreach (Stream target in _targets)
			{
				await target
					.WriteAsync(buffer.AsMemory(offset, result), cancellationToken)
					.ConfigureAwait(false);
			}

			return result;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			int result = await _source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
			foreach (Stream target in _targets)
			{
				await target
					.WriteAsync(buffer.Slice(0, result), cancellationToken)
					.ConfigureAwait(false);
			}

			return result;
		}

		public override int ReadByte()
		{
			int result = _source.ReadByte();

			if (result != -1)
			{
				foreach (Stream target in _targets)
				{
					target.WriteByte((byte)result);
				}
			}

			return result;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotImplementedException();
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public override void WriteByte(byte value)
		{
			throw new NotImplementedException();
		}

		public override bool CanRead => _source.CanRead;
		public override bool CanSeek => _source.CanSeek;
		public override bool CanWrite => _source.CanWrite;
		public override long Length => _source.Length;

		public override long Position
		{
			get => _source.Position;
			set => _source.Position = value;
		}
	}
}
