using System;
using System.Threading;

using OKX.Api;

namespace RoverBot
{
	public static class Program
	{
		public const string CheckLine = "******************************************************************************";

		public const string Version = "2.12";
		
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public const string SecretKey = "A9D9708CD86F133CCFA6AD8D5998AAC9";

		public const string PassPhrase = "FTY19-641TD-331Eq";

		public static void Main()
		{
			Logger.Write(CheckLine);

			Logger.Write(string.Format("RoverBot Version {0} Started", Version));

			Logger.Write(ApiKey);

			//Exchange.GetBalance(out var balance);

			var Client = new OKXRestApiClient();

			var Socket = new OKXWebSocketApiClient();

			Socket.SetApiCredentials(ApiKey, SecretKey, PassPhrase);

			Client.SetApiCredentials(ApiKey, SecretKey, PassPhrase);

			var handler = new PriceHandler(Socket, "BTC-USDT-SWAP");

			while(true)
			{
				if(handler.GetHandlerState())
				{
					Console.WriteLine(handler.GetAskPrice());
				}
				else
				{
					Console.WriteLine("invalid handler");
				}

				Thread.Sleep(1000);
			}
		}
	}
}
