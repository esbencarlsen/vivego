using System;

using vivego.EventStore;

using Range = vivego.EventStore.Range;

namespace vivego.Collection.EventStore
{
	public static class RequestHandlerHelper
	{
		public static string MakeKey(string id, long version) => $"{id}_{version}";

		public static (long start, long end) GetAbsoluteStartEnd(EventStoreState state, Range range)
		{
			if (state is null) throw new ArgumentNullException(nameof(state));
			if (range is null) throw new ArgumentNullException(nameof(range));

			if (state.Version < 0)
			{
				return (0, -1);
			}

			long start;
			if (range.Start < 0)
			{
				// Index from behind
				start = state.Version + range.Start + 1;
			}
			else
			{
				start = range.Start;
			}

			if (start > state.Version)
			{
				return (0, -1);
			}

			long end;
			if (range.End < 0)
			{
				// Index from behind
				end = state.Version + range.End + 1;
			}
			else
			{
				end = range.End;
			}

			if (start > end)
			{
				return (0, -1);
			}

			if (end > state.Version)
			{
				end = state.Version;
			}

			return (start, end);
		}
	}
}