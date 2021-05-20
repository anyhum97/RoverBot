using System;

namespace RoverBot
{
	public static class TradeBot
	{
		public const string StartLine = "******************************************************************************";

		public const string ApiKey = "RuqJRpQ62CErjxJp7OTwShtiFFFS8mdsSPOTgtKXFWUr6lBeqk0T0UA4B463pttL";

		public const string SecretKey = "BrBD2UJbYdxUxCBsRFGqV9zK6HLZHQjbHtKEoME7LByXYgGzlrD7oHsf9zWUGkLL";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.212";

		public static string Symbol = Currency2 + Currency1;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.001m;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 3;

		public static bool IsValid()
		{
			try
			{


				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("IsValid: " + exception.Message);

				return false;
			}
		}
	}
}

