using System;
using System.Linq;
using System.Threading;

namespace RoverBot
{
	public class Program
	{
		public static void Main()
		{
			try
			{
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

