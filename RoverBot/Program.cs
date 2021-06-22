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
                BinanceFutures.IsValid();

				Thread.Sleep(8000);

				BinanceFutures.OnEntryPointDetected();

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

