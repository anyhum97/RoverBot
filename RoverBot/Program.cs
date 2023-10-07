using System;
using System.Threading;
using System.Linq;

using OKX.Api;

namespace RoverBot
{
	public static class Program
	{
		public const string CheckLine = "******************************************************************************";

		public const string Version = "2.43";
		
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public const string SecretKey = "A9D9708CD86F133CCFA6AD8D5998AAC9";

		public const string PassPhrase = "FTY19-641TD-331Eq";

		public static void Main()
		{
			Logger.Write(CheckLine);

			Logger.Write(string.Format("RoverBot Version {0} Started", Version));

			Logger.Write(ApiKey);

			var Exchange = new Exchange("BTC-USDT-SWAP", OKX.Api.Enums.OkxInstrumentType.Swap);

			while(true)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
