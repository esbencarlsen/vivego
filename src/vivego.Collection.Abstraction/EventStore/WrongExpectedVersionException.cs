using System;

namespace vivego.Collection.EventStore
{
	/// <summary>
	/// Exception thrown if the expected version specified on an operation does not match the version of the stream when the operation was attempted.
	/// </summary>
	public sealed class WrongExpectedVersionException : Exception
	{
		public long? ExpectedVersion { get; }
		public long? ActualVersion { get; }

		public WrongExpectedVersionException(string message, long? expectedVersion, long? actualVersion) : base(message)
		{
			ExpectedVersion = expectedVersion;
			ActualVersion = actualVersion;
		}

		public WrongExpectedVersionException()
		{
		}

		public WrongExpectedVersionException(string message) : base(message)
		{
		}

		public WrongExpectedVersionException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
