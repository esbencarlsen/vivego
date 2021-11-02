using System;

namespace vivego.Collection.EventStore
{
	public interface IRecordedEvent<out T>
	{
		string Id { get; }
		
		/// <summary>The number of this event in the stream</summary>
		long EventNumber { get; }

		/// <summary>
		/// A datetime representing when this event was created in the system
		/// </summary>
		DateTimeOffset CreatedAt { get; }

		/// <summary>The type of event this is</summary>
		string Type { get; }

		T Value { get; }
	}
}