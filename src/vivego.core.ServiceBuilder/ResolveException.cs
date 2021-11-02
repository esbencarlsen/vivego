using System;
using System.Runtime.Serialization;

namespace vivego.ServiceBuilder
{
	public sealed class ResolveException : Exception
	{
		public ResolveException()
		{
		}

		public ResolveException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public ResolveException(string? message) : base(message)
		{
		}

		public ResolveException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
