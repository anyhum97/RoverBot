using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

namespace RoverBot
{
	public class TradeParams
	{
		public readonly DateTime Time;

		public readonly decimal Result;

		public readonly decimal Factor1;

		public readonly decimal Factor2;		

		public readonly int Stack;

		public TradeParams(DateTime time, decimal factor1, decimal factor2, int stack, decimal result)
		{
			try
			{
				Time = time;

				Result = result;

				Factor1 = factor1;

				Factor2 = factor2;

				Stack = stack;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams: " + exception.Message);
			}
		}

		public static bool ReadList(string path, out List<TradeParams> history)
		{
			history = new List<TradeParams>();

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
					if(TryParse(lines[i], out var trade))
					{
						history.Add(trade);
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.ReadList: " + exception.Message);

				return false;
			}
		}

		public static bool WriteList(string path, List<TradeParams> history)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				
				for(int i=0; i<history.Count; ++i)
				{
					stringBuilder.Append(history[i].Format());

					stringBuilder.Append("\n");
				}
				
				File.WriteAllText(path, stringBuilder.ToString());

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.WriteList: " + exception.Message);

				return false;
			}
		}

		public static bool AppendList(string path, List<TradeParams> history)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				
				for(int i=0; i<history.Count; ++i)
				{
					stringBuilder.Append(history[i].Format());

					stringBuilder.Append("\n");
				}
				
				File.AppendAllText(path, stringBuilder.ToString());

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.AppendList: " + exception.Message);

				return false;
			}
		}

		public static bool Append(string path, TradeParams trade)
		{
			try
			{
				File.AppendAllText(path, trade.Format() + "\n");

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.Append: " + exception.Message);

				return false;
			}
		}
		
		private static bool TryParse(string str, out TradeParams trade)
		{
			trade = default;

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

				trade = new TradeParams(time, values[0], values[1], (int)values[2], values[3]);

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.TryParse: " + exception.Message);

				return false;
			}
		}

		private string Format()
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:dd.MM.yyyy HH:mm}\t{1:F2}\t{2:F2}\t{3}\t{4:F2}", Time, Factor1, Factor2, Stack, Result);
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.Format: " + exception.Message);

				return "Invalid Format";
			}
		}

		public override string ToString()
		{
			try
			{
				return string.Format("({0:F2}, {1:F2}, {2}) => {3:F2}", Factor1, Factor2, Stack, Result);
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.ToString: " + exception.Message);

				return "Invalid Format";
			}
		}

		public bool IsValid()
		{
			try
			{
				if(Result <= 0.0m)
				{
					return false;
				}

				if(Factor1 <= 0.0m)
				{
					return false;
				}

				if(Factor2 <= 0.0m)
				{
					return false;
				}

				if(Stack <= 0)
				{
					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TradeParams.IsValid: " + exception.Message);

				return false;
			}
		}
	}
}

