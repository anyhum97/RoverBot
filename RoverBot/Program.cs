using System;
using System.Threading;

namespace RoverBot
{
	public class Program
	{
		public const string CheckLine = "******************************************************************************";

		public static void Main()
		{
			try
			{
				Logger.Write(CheckLine);

				BinanceFutures.StartRoverBotAsync().Wait();

				while(BinanceFutures.IsValid())
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

