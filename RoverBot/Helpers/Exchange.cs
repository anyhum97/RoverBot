using System;
using System.Linq;

using OKX.Api;

namespace RoverBot
{
	public static class Exchange
	{
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public const string SecretKey = "A9D9708CD86F133CCFA6AD8D5998AAC9";

		public const string PassPhrase = "FTY19-641TD-331Eq";

		public static decimal AvailableBalance { get; private set; }

		public static decimal FrozenBalance { get; private set; }

		private static readonly OKXRestApiClient Client;

		static Exchange()
		{
			try
			{
				Client = new OKXRestApiClient();

				Client.SetApiCredentials(ApiKey, SecretKey, PassPhrase);
			}
			catch(Exception exception)
			{
				Logger.Write("Exchange: " + exception.Message);
			}
		}
	}
}
