using System.Text.Json.Serialization;

namespace RoverBot
{
	public class BookTickerStream
	{
		[JsonPropertyName("stream")]
		public string Stream { get; set; }

		[JsonPropertyName("data")]
		public BookTicker Data { get; set; }
	}
}

