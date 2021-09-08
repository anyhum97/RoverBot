using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RoverBot
{
	public class BookTicker
	{
		[JsonPropertyName("e")]
		public string EventType { get; set; }

		[JsonPropertyName("u")]
		public long OrderBookId { get; set; }

		[JsonPropertyName("s")]
		public string Symbol { get; set; }

		[JsonPropertyName("b")]
		public string BestBidPrice { get; set; }

		[JsonPropertyName("B")]
		public string BestBidQty { get; set; }

		[JsonPropertyName("a")]
		public string BestAskPrice { get; set; }

		[JsonPropertyName("A")]
		public string BestAskQty { get; set; }

		[JsonPropertyName("T")]
		public long TradeTime { get; set; }

		[JsonPropertyName("E")]
		public long EventTime { get; set; }

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		public bool GetPrice(out decimal price)
		{
			price = default;

			try
			{
				if(decimal.TryParse(BestBidPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
				{
					return true;
				}
				else
				{
					Logger.Write("BookTicker.GetPrice: Invalid Input");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("BookTicker.GetPrice: " + exception.Message);

				return false;
			}
		}

		public bool GetTime(out DateTime time)
		{
			time = default;

			try
			{
				return DateTimeFromTimeStamp(EventTime, out time);
			}
			catch(Exception exception)
			{
				Logger.Write("BookTicker.GetTime: " + exception.Message);

				return false;
			}
		}

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
				Logger.Write("BookTicker.DateTimeFromTimeStamp: " + exception.Message);

				return false;
			}
		}
	}
}

