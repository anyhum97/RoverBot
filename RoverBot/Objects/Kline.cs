using System.Text.Json.Serialization;

namespace RoverBot
{
	public class Kline
	{
		[JsonPropertyName("t")]
		public long StartTimeStamp { get; set; }

		[JsonPropertyName("T")]
		public long StopTimeStamp { get; set; }

		[JsonPropertyName("s")]
		public string Symbol { get; set; }

		[JsonPropertyName("i")]
		public string Interval { get; set; }

		[JsonPropertyName("f")]
		public long FirstTradeId { get; set; }

		[JsonPropertyName("L")]
		public long LastTradeId { get; set; }

		[JsonPropertyName("o")]
		public string OpenPrice { get; set; }

		[JsonPropertyName("c")]
		public string ClosePrice { get; set; }

		[JsonPropertyName("h")]
		public string HighPrice { get; set; }

		[JsonPropertyName("l")]
		public string LowPrice { get; set; }

		[JsonPropertyName("v")]
		public string Volume { get; set; }

		[JsonPropertyName("n")]
		public long Count { get; set; }

		[JsonPropertyName("x")]
		public bool IsClosed { get; set; }

		[JsonPropertyName("q")]
		public string QuoteVolume { get; set; }

		[JsonPropertyName("V")]
		public string TakerVolume { get; set; }

		[JsonPropertyName("Q")]
		public string TakerQuoteVolume { get; set; }

		[JsonPropertyName("B")]
		public string Ignore { get; set; }
	}
}

