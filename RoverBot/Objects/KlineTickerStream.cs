using System.Text.Json.Serialization;

using RoverBot;

namespace RoverBot
{
	public class KlineTickerStream
	{
		[JsonPropertyName("stream")]
		public string Stream { get; set; }

		[JsonPropertyName("data")]
		public KlineTicker Data { get; set; }
	}
}

