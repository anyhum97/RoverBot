using System;
using System.Globalization;

namespace RoverBot
{
	public readonly struct Kline
	{
		public readonly DateTime CloseTime;
		
		public readonly decimal Open;

		public readonly decimal Close;

		public readonly decimal Low;

		public readonly decimal High;

		public Kline(DateTime closeTime, decimal open, decimal close, decimal low, decimal high)
		{
			CloseTime = closeTime;

			Open = open;
			Close = close;
			Low = low;
			High = high;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm:ss}: Open: {1:F4}, Close: {2:F4}, Low: {3:F4}, High: {4:F4}", CloseTime, Open, Close, Low, High);
		}
	}
}
