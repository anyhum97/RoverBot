using System;

namespace RoverBot
{
	public class Program
	{
		public static void Main()
		{
			StrategyBuilder.UpdateStrategy("BNBUSDT", 1000.0m, out var trade);
		}
	}
}

