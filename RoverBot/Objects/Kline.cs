using System;
using System.Collections.Generic;
using System.Text;

namespace RoverBot
{
	public readonly struct Kline
	{
		public readonly decimal Open;

		public readonly decimal Close;

		public readonly decimal Low;

		public readonly decimal High;

		public readonly DateTime CloseTime;

		public static bool WriteKlineList(List<Kline> list)
		{
			try
			{
				if(list == default)
				{
					Logger.Write("Kline.WriteKlineList: Invalid Kline List");

					return false;
				}

				StringBuilder stringBuilder = new StringBuilder();

				foreach(var record in list)
				{
					stringBuilder.AppendLine(string.Format("{0}: {1} {2} {3} {4}"));
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("Kline.WriteKlineList: {0}", exception.Message));

				return true;
			}
		}
	}
}
