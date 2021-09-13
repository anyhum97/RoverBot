using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

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

		public static bool WriteList(string path, List<Candle> history)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				
				for(int i=default; i<history.Count; ++i)
				{
					stringBuilder.Append(history[i].Format());

					if(i < history.Count - 1)
					{
						stringBuilder.Append("\n");
					}
				}
				
				File.WriteAllText(path, stringBuilder.ToString());

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.WriteList: " + exception.Message);

				return false;
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

		private string Format()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm}\t{1:F2}", CloseTime, Close);
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.Format: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

