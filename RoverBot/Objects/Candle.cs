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

		public static bool ReadList(string path, out List<Candle> history)
		{
			history = new List<Candle>();

			try
			{
				if(File.Exists(path) == false)
				{
					return false;
				}

				string str = File.ReadAllText(path);

				string[] lines = str.Split('\n');

				for(int i=0; i<lines.Length; ++i)
				{
					if(TryParse(lines[i], out var candle))
					{
						history.Add(candle);
					}
					else
					{
						return false;
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.ReadList: " + exception.Message);

				return false;
			}
		}

		public static bool WriteList(string path, List<Candle> history)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				
				for(int i=0; i<history.Count; ++i)
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
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM HH:mm} Price: {1:F6}", CloseTime, Close);
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.ToString: " + exception.Message);

				return "Invalid Format";
			}
		}

		private static bool TryParse(string str, out Candle candle)
		{
			candle = default;

			try
			{
				if(str == null)
				{
					return false;
				}

				if(str.Length > 256)
				{
					str = str.Substring(0, 256);
				}

				string[] parts = str.Split('\t');

				if(parts.Length != 5)
				{
					return false;
				}

				if(DateTime.TryParse(parts[0], out DateTime time) == false)
				{
					return false;
				}

				decimal[] values = new decimal[4];

				for(int i=0; i<4; ++i)
				{
					if(decimal.TryParse(parts[i+1], NumberStyles.Number, CultureInfo.InvariantCulture, out values[i]) == false)
					{
						return false;
					}

					if(values[i] < 0.0m)
					{
						return false;
					}
				}

				candle = new Candle(time, values[0], values[1], values[2], values[3]);

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.TryParse: " + exception.Message);

				return false;
			}
		}

		private string Format()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm}\t{1:F8}\t{2:F8}\t{3:F8}\t{4:F8}", CloseTime, Open, Close, Low, High);
			}
			catch(Exception exception)
			{
				Logger.Write("Candle.Format: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

