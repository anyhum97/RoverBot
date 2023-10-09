using System;
using System.Threading;

using OKX.Api.Enums;

namespace RoverBot
{
	public static class Program
	{
		public const string CheckLine = "******************************************************************************";

		public const string Version = "2.58";
		
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public static void Main()
		{
			Logger.Write(CheckLine);

			Logger.Write(string.Format("RoverBot Version {0} Started", Version));

			Logger.Write(ApiKey);

			var exchange = new Exchange("BTC-USDT-SWAP", OkxInstrumentType.Swap, 10);

			while(true)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
