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
					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("BinanceBookTicker.GetPrice: " + exception.Message);

				return false;
			}
		}
	}
}

