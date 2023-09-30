using System;

using OKX.Api;

namespace RoverBot
{
	public static class Exchange
	{
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public const string SecretKey = "A9D9708CD86F133CCFA6AD8D5998AAC9";

		public const string PassPhrase = "FTY19-641TD-331Eq";

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

		public static bool GetBalance(out decimal balance)
		{
			balance = default;

			try
			{
				var task = Client.TradingAccount.GetAccountBalanceAsync();

				task.Wait();

				if(task.Result.Success)
				{
					
					return true;
				}
				
				Logger.Write("GetBalance: " + task.Result.Error.Message);

				return false;
			}
			catch(Exception exception)
			{
				Logger.Write("GetBalance: " + exception.Message);

				return false;
			}
		}
	}
}
