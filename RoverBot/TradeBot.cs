using System;
using System.Globalization;
using System.Text;
using System.Threading;

using Binance.Net;

namespace RoverBot
{
	public static class TradeBot
	{
		public const string StartLine = "******************************************************************************";

		public const string ApiKey = "RuqJRpQ62CErjxJp7OTwShtiFFFS8mdsSPOTgtKXFWUr6lBeqk0T0UA4B463pttL";

		public const string SecretKey = "BrBD2UJbYdxUxCBsRFGqV9zK6HLZHQjbHtKEoME7LByXYgGzlrD7oHsf9zWUGkLL";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.224";

		public static string Symbol = Currency2 + Currency1;
		
		public const decimal MinNotional = 10.00m;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.001m;

		public const int NotionalPrecision = 4;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 3;

		public static decimal Balance1 { get; private set; } = default;

		public static decimal Balance2 { get; private set; } = default;

		public static decimal TotalBalance { get; private set; } = default;

		public static decimal Frozen { get; private set; } = default;

		public static decimal FeeCoins { get; private set; } = default;

		public static bool IsTrading { get; set; } = default;

		private static BinanceClient Client = default;

		private static TradeParams Trade = default;

		public static void Start()
		{
			try
			{
				Logger.Write(StartLine);

				Logger.Write("RoverBot Version " + Version + " Started");

				Client = new BinanceClient();

				Client.SetApiCredentials(ApiKey, SecretKey);
				
				WebSocketSpot.StartPriceStream();

				TelegramBot.Start();

				while(true)
				{
					if(UpdateBalance())
					{
						decimal balance = TotalBalance;

						if(balance < MinNotional)
						{
							balance = 1000.0m;
						}

						if(StrategyBuilder.UpdateStrategy(Symbol, balance, out var trade))
						{
							IsTrading = true;

							Trade = trade;

							break;
						}
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("Start: " + exception.Message);

				Client = null;
			}
		}

		public static bool IsValid()
		{
			try
			{
				if(Client == null)
				{
					return false;
				}

				if(Trade == null)
				{
					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("IsValid: " + exception.Message);

				return false;
			}
		}

		public static bool UpdateBalance()
		{
			try
			{
				if(Client == null)
				{
					return false;
				}

				decimal price = WebSocketSpot.CurrentPrice;

				if(price == default)
				{
					Thread.Sleep(1000);

					return false;
				}

				var accountInfo = Client.General.GetAccountInfo();

				if(accountInfo.Success)
				{
					bool find1 = false;

					bool find2 = false;

					decimal total1 = default;

					decimal total2 = default;

					decimal bnb = default;

					foreach(var record in accountInfo.Data.Balances)
					{
						if(record.Asset.Contains("BNB"))
						{
							bnb = record.Free;
						}

						if(find1 == false)
						{
							if(record.Asset.Contains(Currency1))
							{
								Balance1 = record.Free;

								total1 = record.Total;

								find1 = true;
							}
						}

						if(find2 == false)
						{
							if(record.Asset.Contains(Currency2))
							{
								Balance2 = record.Free;

								total2 = record.Total;

								find2 = true;
							}
						}

						if(find1 && find2)
						{
							TotalBalance = total1 + total2*price;

							StringBuilder stringBuilder = new StringBuilder();

							stringBuilder.Append("UpdateBalance: ");

							stringBuilder.Append(Format(Balance1, 4));
							stringBuilder.Append(" ");
							stringBuilder.Append(Currency1);
							stringBuilder.Append(", ");

							stringBuilder.Append(Format(Balance2, 6));
							stringBuilder.Append(" ");
							stringBuilder.Append(Currency2);

							if(Currency2 != "BNB")
							{
								stringBuilder.Append(", ");
								stringBuilder.Append(Format(bnb, 4));
								stringBuilder.Append(" BNB");
							}

							stringBuilder.Append(", Total = ");
							stringBuilder.Append(Format(TotalBalance, 2));
							stringBuilder.Append(" USDT");

							Logger.Write(stringBuilder.ToString());

							FeeCoins = bnb;

							return true;
						}
					}

					return false;
				}
				else
				{
					Logger.Write("UpdateBalance: Bad Request");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateBalance: " + exception.Message);
			
				return false;
			}
		}

		private static string Format(decimal value, int sign = 4)
		{
			try
			{
				sign = Math.Max(sign, 0);
				sign = Math.Min(sign, 8);

				return string.Format(CultureInfo.InvariantCulture, "{0:F" + sign + "}", value);
			}
			catch(Exception exception)
			{
				Logger.Write("Format: " + exception.Message);
			
				return "Invalid Format";
			}
		}

		private static string Point(decimal value)
		{
			try
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}", value);
			}
			catch(Exception exception)
			{
				Logger.Write("Point: " + exception.Message);
			
				return "Invalid Format";
			}
		}
	}
}

