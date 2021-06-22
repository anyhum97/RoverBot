using System;
using System.Globalization;

namespace RoverBot
{
	public class Candle
	{
		public readonly DateTime CloseTime;

		public readonly decimal Open;
		public readonly decimal Close;
		public readonly decimal Low;
		public readonly decimal High;

		public Candle(DateTime closeTime, decimal open, decimal close, decimal low, decimal high)
		{
			try
			{
				CloseTime = closeTime;

				Open = open;
				Close = close;
				Low = low;
				High = high;
			}
			catch(Exception exception)
			{
				Logger.Write("Candle: " + exception.Message);
			}
		}

		public override string ToString()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM HH:mm} Price: {1:F2}", CloseTime, Close);
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.ToString: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

