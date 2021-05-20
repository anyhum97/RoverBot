using System;
using System.Threading;

namespace RoverBot
{
	public class Program
	{
		public static void Main()
		{
			try
			{
				WebSocketFutures.StartPriceStream();

				while(TradeBot.IsValid())
				{
					Thread.Sleep(1000);
				}
			}
			catch(Exception exception)
			{
				Logger.Write(exception.Message);
			}
		}
	}
}

