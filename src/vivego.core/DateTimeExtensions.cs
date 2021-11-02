using System;
using System.Collections.Generic;
using System.Globalization;

namespace vivego.core
{
	public static class DateTimeExtensions
	{
		public const long TicksPerMs = TimeSpan.TicksPerSecond / 1000;
		public const long UnixEpoch = 621355968000000000L;

		/// <summary>
		///     The number of ticks per microsecond.
		/// </summary>
		public const int TicksPerMicrosecond = 10;

		/// <summary>
		///     The number of ticks per Nanosecond.
		/// </summary>
		public const int NanosecondsPerTick = 100;

		private static readonly DateTime s_unixEpochDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static bool Between(this DateTime dateTime, DateTime from, DateTime to, bool inclusiveTo)
		{
			if (inclusiveTo)
			{
				return dateTime >= from && dateTime <= to;
			}

			return dateTime >= from && dateTime < to;
		}

		public static bool Between(this DateTimeOffset dateTime, DateTimeOffset from, DateTimeOffset to, bool inclusiveTo)
		{
			if (inclusiveTo)
			{
				return dateTime >= from && dateTime <= to;
			}

			return dateTime >= from && dateTime < to;
		}

		public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
		{
			return timeSpan == TimeSpan.Zero
				? dateTime
				: dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
		}

		public static DateTimeOffset Truncate(this DateTimeOffset dateTime, TimeSpan timeSpan)
		{
			return timeSpan == TimeSpan.Zero
				? dateTime
				: dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
		}

		private static readonly TimeSpan s_oneMillisecond = TimeSpan.FromMilliseconds(1);
		private static readonly TimeSpan s_oneSecond = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan s_oneHour = TimeSpan.FromHours(1);
		private static readonly TimeSpan s_oneDay = TimeSpan.FromDays(1);

		public static DateTime Floor(this DateTime dateTime, DateTimeResolutions resolution)
		{
			switch (resolution)
			{
				case DateTimeResolutions.Millisecond:
					return dateTime.Truncate(s_oneMillisecond);
				case DateTimeResolutions.Second:
					return dateTime.Truncate(s_oneSecond);
				case DateTimeResolutions.Minute:
					return dateTime.Truncate(s_oneMinute);
				case DateTimeResolutions.Hour:
					return dateTime.Truncate(s_oneHour);
				case DateTimeResolutions.Day:
					return dateTime.Truncate(s_oneDay);
				case DateTimeResolutions.Month:
					return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
				case DateTimeResolutions.Year:
					return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
				default:
					throw new NotSupportedException(resolution.ToString());
			}
		}

		public static DateTime FromUnixTime(this double unixTime)
		{
			return s_unixEpochDateTime + TimeSpan.FromSeconds(unixTime);
		}

		public static DateTime FromUnixTimeMs(this double msSince1970)
		{
			long ticks = (long)(UnixEpoch + msSince1970 * TicksPerMs);
			return new DateTime(ticks, DateTimeKind.Utc).ToUniversalTime();
		}

		public static DateTime Parse(string input)
		{
			return DateTime.Parse(input, CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
		}

		public static long ToUnixTime(this DateTime dateTime)
		{
			long epoch = (dateTime.ToUniversalTime().Ticks - UnixEpoch) / TimeSpan.TicksPerSecond;
			return epoch;
		}

		public static long ToUnixTimeMs(this DateTime dateTime)
		{
			long epoch = (dateTime.Ticks - UnixEpoch) / TicksPerMs;
			return epoch;
		}

		public static bool TryParseInvariantUniversal(string input, out DateTime dt)
		{
			return DateTime.TryParse(input, CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt);
		}

		public static int SecondFragment(this DateTime self)
		{
			int fraction = (int)(self.Ticks % 10000000);
			return fraction;
		}

		public static bool HasExpired(this DateTime? expiresAt,
			DateTime? now = null)
		{
			return HasExpired(expiresAt, out TimeSpan? _, now);
		}

		public static bool HasExpired(this DateTimeOffset? expiresAt,
			DateTimeOffset? now = null)
		{
			return HasExpired(expiresAt, out TimeSpan? _, now);
		}

		public static bool HasExpired(this DateTime? expiresAt,
			out TimeSpan? timeToLive,
			DateTime? now = null)
		{
			if (expiresAt.HasValue)
			{
				timeToLive = expiresAt.Value.Subtract(now.GetValueOrDefault(DateTime.UtcNow));
				return timeToLive <= TimeSpan.Zero;
			}

			timeToLive = null;
			return false;
		}

		public static bool HasExpired(this DateTimeOffset? expiresAt,
			out TimeSpan? timeToLive,
			DateTimeOffset? now = null)
		{
			if (expiresAt.HasValue)
			{
				timeToLive = expiresAt.Value.Subtract(now.GetValueOrDefault(DateTime.UtcNow));
				return timeToLive <= TimeSpan.Zero;
			}

			timeToLive = null;
			return false;
		}

		public static bool HasExpired(this DateTime expiresAt,
			DateTime? now = null)
		{
			return HasExpired(expiresAt, out TimeSpan? _, now);
		}

		public static bool HasExpired(this DateTime expiresAt,
			out TimeSpan? timeToLive,
			DateTime? now = null)
		{
			timeToLive = expiresAt.Subtract(now.GetValueOrDefault(DateTime.UtcNow));
			return timeToLive <= TimeSpan.Zero;
		}

		public static bool HasExpired(this DateTimeOffset expiresAt,
			out TimeSpan? timeToLive,
			DateTimeOffset? now = null)
		{
			timeToLive = expiresAt.Subtract(now.GetValueOrDefault(DateTime.UtcNow));
			return timeToLive <= TimeSpan.Zero;
		}
	}

	/// <summary>
	///     Represents the resolution with which to floor or ceil. This could be to ceil to the nearest hour, day, month or
	///     year
	/// </summary>
	[Flags]
	public enum DateTimeResolutions
	{
		Millisecond = 0,
		Second = 1,
		Minute = 2,
		Hour = 4,
		Day = 8,
		Month = 16,
		Year = 32
	}

	public static class DateTimeResolutionExtensions
	{
		public static TimeSpan ToTimeSpan(this DateTimeResolutions dateTimeResolution)
		{
			switch (dateTimeResolution)
			{
				case DateTimeResolutions.Millisecond:
					return TimeSpan.FromMilliseconds(1);
				case DateTimeResolutions.Second:
					return TimeSpan.FromSeconds(1);
				case DateTimeResolutions.Minute:
					return TimeSpan.FromMinutes(1);
				case DateTimeResolutions.Hour:
					return TimeSpan.FromHours(1);
				case DateTimeResolutions.Day:
					return TimeSpan.FromDays(1);
				case DateTimeResolutions.Month:
					return TimeSpan.FromDays(30);
				case DateTimeResolutions.Year:
					return TimeSpan.FromDays(365);
				default:
					throw new ArgumentOutOfRangeException(nameof(dateTimeResolution), dateTimeResolution, null);
			}
		}

		public static IEnumerable<DateTime> TimeRange(DateTime from, DateTime to, DateTimeResolutions resolution)
		{
			from = from.Floor(resolution);
			while (from <= to)
			{
				yield return from;
				switch (resolution)
				{
					case DateTimeResolutions.Millisecond:
						from = from.Add(TimeSpan.FromMilliseconds(1));
						break;
					case DateTimeResolutions.Second:
						from = from.Add(TimeSpan.FromSeconds(1));
						break;
					case DateTimeResolutions.Minute:
						from = from.Add(TimeSpan.FromMinutes(1));
						break;
					case DateTimeResolutions.Hour:
						from = from.Add(TimeSpan.FromHours(1));
						break;
					case DateTimeResolutions.Day:
						from = from.Add(TimeSpan.FromDays(1));
						break;
					case DateTimeResolutions.Month:
						from = from.Add(TimeSpan.FromDays(30));
						break;
					case DateTimeResolutions.Year:
						from = from.Add(TimeSpan.FromDays(365));
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
				}
			}
		}
	}
}
