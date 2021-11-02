using System;

namespace vivego.Scheduler
{
	public sealed class DefaultSchedulerOptions
	{
		public int DegreeOfParallelism { get; set; } = Environment.ProcessorCount;
#if DEBUG
		public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(5);
#else
		public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
#endif
	}
}
