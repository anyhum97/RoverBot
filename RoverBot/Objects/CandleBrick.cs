using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

namespace RoverBot
{
	public class CandleBrick : Candle
	{
		public readonly decimal Average;

		public readonly decimal Deviation;

		public CandleBrick(DateTime closeTime, decimal open, decimal close, decimal low, decimal high, decimal average, decimal deviation) : base(closeTime, open, close, low, high)
		{
			try
			{
				Average = average;

				Deviation = deviation;
			}
			catch(Exception exception)
			{
				Logger.Write("CandleBrick: " + exception.Message);
			}
		}
		
		public static bool ReadList(string path, out List<CandleBrick> history)
		{
			history = new List<CandleBrick>();

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
					if(TryParse(lines[i], out var candleBrick))
					{
						history.Add(candleBrick);
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
				Logger.Write("CandleBrick.ReadList: " + exception.Message);

				return false;
			}
		}

		public static bool WriteList(string path, List<CandleBrick> history)
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
				Logger.Write("CandleBrick.WriteList: " + exception.Message);

				return false;
			}
		}

		public static bool TryParse(string str, out CandleBrick candleBrick)
		{
			candleBrick = default;

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

				if(parts.Length != 7)
				{
					return false;
				}

				if(DateTime.TryParse(parts[0], out DateTime time) == false)
				{
					return false;
				}

				decimal[] values = new decimal[6];

				for(int i=0; i<6; ++i)
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

				candleBrick = new CandleBrick(time, values[0], values[1], values[2], values[3], values[4], values[5]);

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("CandleBrick.TryParse: " + exception.Message);

				return false;
			}
		}
		
		public override string ToString()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM HH:mm} Price: {1:F6} (Average: {2:F6}, Deviation: {3:F6})", CloseTime, Close, Average, Deviation);
			}
			catch(Exception exception)
			{
				Logger.Write("CandleBrick.ToString: " + exception.Message);

				return "Invalid Format";
			}
		}

		private string Format()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm}\t{1:F8}\t{2:F8}\t{3:F8}\t{4:F8}\t{5:F8}\t{6:F8}", CloseTime, Open, Close, Low, High, Average, Deviation);
			}
			catch(Exception exception)
			{
				Logger.Write("CandleBrick.Format: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

