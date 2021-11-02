using System;

using MediatR;

namespace vivego.Collection.Queue.Prepend
{
	public sealed record PrependRequest : IRequest<long?>
	{
		public string Id { get; }
		public byte[] Data { get; }
		public long? ExpectedVersion  { get; }
		public TimeSpan? ExpiresIn { get; }

		public PrependRequest(
			string id,
			byte[] data,
			long? expectedVerson = default,
			TimeSpan? expiresIn = default)
		{
			Id = id;
			Data = data;
			ExpectedVersion = expectedVerson;
			ExpiresIn = expiresIn;
		}
	}
}