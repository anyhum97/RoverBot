using System;
using System.Text.Json.Serialization;
using System.Globalization;

namespace RoverBot
{
	public class KlineTicker
	{
		[JsonPropertyName("e")]
		public string EventType { get; set; }

		[JsonPropertyName("E")]
		public long EventTime { get; set; }

		[JsonPropertyName("s")]
		public string Symbol { get; set; }

		[JsonPropertyName("k")]
		public Kline Kline { get; set; }

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		private static bool DateTimeFromTimeStamp(long timestamp, out DateTime time)
		{
			time = default;

			try
			{
				time = Epoch.AddMilliseconds(timestamp).ToLocalTime();

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("KlineTicker.DateTimeFromTimeStamp: " + exception.Message);

				return false;
			}
		}

		public bool GetTime(out DateTime time)
		{
			time = default;

			try
			{
				return DateTimeFromTimeStamp(Kline.StopTimeStamp, out time);
			}
			catch(Exception exception)
			{
				Logger.Write("KlineTicker.GetTime: " + exception.Message);

				return false;
			}
		}

		public bool GetPrice(out decimal price)
		{
			price = default;

			try
			{
				return decimal.TryParse(Kline.ClosePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
			}
			catch(Exception exception)
			{
				Logger.Write("KlineTicker.GetPrice: " + exception.Message);

				return false;
			}
		}
	}
}

