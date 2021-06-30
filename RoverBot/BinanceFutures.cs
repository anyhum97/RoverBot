using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using Binance.Net.Objects.Futures.MarketData;

using Timer = System.Timers.Timer;

namespace RoverBot
{
	public static class BinanceFutures
	{
		public const string CheckLine = "******************************************************************************";

		public const string ApiKey = "znnURJsV8h8EKDGfFw6kqoT5cJFsl21hCHzdkXJDWcBBT3hpdn1UHwJbTiGOw7Sc";

		public const string SecretKey = "wQOJwAul4Cse8oKqCCCKBpJS23b2Kjdq104PNIHDZ8ogdmN550EjnjDJbuHbiG3l";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.885(a)";

		public static string Symbol = Currency2 + Currency1;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.001m;

		public const int DefaultLeverage = 10;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 3;

		public static decimal LastBalance { get; private set; } = default;

		public static decimal Balance { get; private set; } = default;

		public static decimal TotalBalance { get; private set; } = default;

		public static decimal Frozen { get; private set; } = default;

		public static decimal FeePrice { get; private set; } = default;

		public static decimal FeeBalance { get; private set; } = default;

		public static decimal FeeCoins { get; private set; } = default;

		public static int CurrentLeverage { get; private set; } = default;

		public static int MaxLeverage { get; private set; } = default;

		public static bool InPosition { get; set; } = false;

		public static bool State { get; private set; } = false;

		public static bool IsTrading { get; set; } = false;

		private static BinanceClient Client = default;

		private static Timer InternalTimer1 = default;

		private static Timer InternalTimer2 = default;

		static BinanceFutures()
		{
			try
			{
				Logger.Write(CheckLine);

				Logger.Write("FuturesBot Version " + Version + " Started");

				Client = new BinanceClient();
				
				Client.SetApiCredentials(ApiKey, SecretKey);
				
				WebSocketFutures.StartPriceStream();
				
				WebSocketFutures.StartKlineStream();
				
				while(IsTrading == false)
				{
					if(CheckPosition(Symbol))
					{
						break;
					}
					
					Thread.Sleep(1000);
				}
				
				State = true;
				
				UpdateBalance();
				
				SetTradeParams();

				StartInternalTimer1();

				StartInternalTimer2();
			}
			catch(Exception exception)
			{
				Logger.Write("BinanceFutures: " + exception.Message);

				Client = default;

				State = default;
			}
		}

		public static bool IsValid()
		{
			try
			{
				if(State == false)
				{
					return false;
				}

				if(Client == null)
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
		
		private static void StartInternalTimer1()
		{
			try
			{
				InternalTimer1 = new Timer();

				InternalTimer1.Interval = 20000;

				InternalTimer1.Elapsed += InternalTimerElapsed1;

				InternalTimer1.Start();
			}
			catch(Exception exception)
			{
				Logger.Write("StartInternalTimer1: " + exception.Message);
			}
		}

		private static void StartInternalTimer2()
		{
			try
			{
				InternalTimer2 = new Timer();

				InternalTimer2.Interval = 600000;

				InternalTimer2.Elapsed += InternalTimerElapsed2;

				InternalTimer2.Start();
			}
			catch(Exception exception)
			{
				Logger.Write("StartInternalTimer2: " + exception.Message);
			}
		}

		private static void InternalTimerElapsed1(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				Task.Run(() =>
				{
					CheckPosition(Symbol);

					UpdateBalance();
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed1: " + exception.Message);
			}
		}

		private static void InternalTimerElapsed2(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				Task.Run(() =>
				{
					SetTradeParams();
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed2: " + exception.Message);
			}
		}

		public static void OnEntryPointDetected(decimal takeProfit)
		{
			try
			{
				if(IsValid())
				{
					if(IsTrading)
					{
						if(InPosition == false)
						{
							if(CurrentLeverage == DefaultLeverage)
							{
								decimal price = WebSocketFutures.CurrentPrice;

								const decimal depositFactor = 0.98m;

								decimal deposit = depositFactor * Balance;

								int deals = (int)Math.Floor(deposit * CurrentLeverage / (price * VolumeFilter));

								decimal volume = deals * VolumeFilter;

								PlaceLongOrder(Symbol, volume, takeProfit);

								CheckPosition(Symbol);

								UpdateBalance();
							}
							else
							{
								Logger.Write("Invalid Leverage");
							}
						}
						else
						{
							Logger.Write("Already InPosition");
						}
					}
					else
					{
						Logger.Write("EntryPoint");
					}
				}
				else
				{
					Logger.Write("EntryPoint");
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnLongEntryPointDetected: " + exception.Message);
			}
		}

		private static bool PlaceLongOrder(string symbol, decimal volume, decimal takeProfit)
		{
			try
			{
				if(IsValid())
				{
					if(WebSocketFutures.CurrentPrice > 0.0m)
					{
						if(symbol == null)
						{
							return false;
						}
						
						volume = Math.Round(volume, VolumePrecision);
						
						if(volume < VolumeFilter)
						{
							return false;
						}

						decimal price = WebSocketFutures.CurrentPrice;

						takeProfit = Math.Round(takeProfit, PricePrecision);
						
						if(takeProfit <= price)
						{
							return false;
						}

						var orders = new BinanceFuturesBatchOrder[2];
						
						orders[0] = new BinanceFuturesBatchOrder()
						{
							Symbol = symbol,
							Side = OrderSide.Buy,
							Type = OrderType.Market,
							PositionSide = PositionSide.Both,
							Quantity = volume,
							ReduceOnly = false,
						};

						orders[1] = new BinanceFuturesBatchOrder()
						{
							Symbol = symbol,
							Side = OrderSide.Sell,
							Type = OrderType.TakeProfitMarket,
							TimeInForce = TimeInForce.GoodTillCancel,
							PositionSide = PositionSide.Both,
							ActivationPrice = takeProfit,
							StopPrice = takeProfit,
							Quantity = volume,
							ReduceOnly = true,
						};
						
						var responce = Client.FuturesUsdt.Order.PlaceMultipleOrders(orders);
						
						if(responce.Success)
						{
							var data = responce.Data.ToArray();
							
							foreach(var record in data)
							{
								if(record.Success == false)
								{
									Logger.Write("PlaceLongOrder: " + record.Error.Message);
								}
							}
							
							Logger.Write("PlaceLongOrder: Success (Price = " + Format(price, PricePrecision) + ")");

							return true;
						}
						else
						{
							Logger.Write("PlaceLongOrder: " + responce.Error.Message);
							
							CancelAllOrders(symbol);
							
							return false;
						}
					}
					else
					{
						Logger.Write("PlaceLongOrder: Invalid Price");
						
						return false;
					}
				}
				else
				{
					Logger.Write("PlaceLongOrder: Invalid Account");
					
					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("PlaceLongOrder: " + exception.Message);

				return false;
			}
		}

		private static bool CancelAllOrders(string symbol)
		{
			try
			{
				const int attempts = 3;

				for(int i=0; i<attempts; ++i)
				{
					var responce = Client.FuturesUsdt.Order.CancelAllOrders(symbol);

					if(responce.Success)
					{
						return true;
					}
					else
					{
						Logger.Write("CancelAllOrders: " + responce.Error.Message);
					}
				}

				return false;
			}
			catch(Exception exception)
			{
				Logger.Write("CancelAllOrders: " + exception.Message);

				return false;
			}
		}

		private static bool UpdateFeePrice()
		{
			try
			{
				var responce = Client.Spot.Market.GetPrice("BNBUSDT");

				if(responce.Success)
				{
					FeePrice = responce.Data.Price;

					return true;
				}
				else
				{
					Logger.Write("UpdateFeePrice: " + responce.Error.Message);

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateFeePrice: " + exception.Message);

				return false;
			}
		}

		private static bool UpdateBalance()
		{
			try
			{
				if(IsValid())
				{
					var responce = Client.FuturesUsdt.Account.GetBalance();

					if(responce.Success)
					{
						var list = responce.Data.ToList();

						for(int i=0; i<list.Count; ++i)
						{
							if(list[i].Asset == Currency1)
							{
								if(TotalBalance != LastBalance)
								{
									LastBalance = TotalBalance;
								}

								Balance = list[i].CrossWalletBalance;

								TotalBalance = list[i].Balance;

								Frozen = TotalBalance - Balance;

								return true;
							}

							if(list[i].Asset == "BNB")
							{
								FeeCoins = list[i].Balance;

								if(FeePrice == default)
								{
									UpdateFeePrice();
								}

								FeeBalance = FeePrice * FeeCoins;
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
				else
				{
					Logger.Write("UpdateBalance: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateBalance: " + exception.Message);

				return false;
			}
		}

		private static bool GetMaxLeverage()
		{
			try
			{
				if(IsValid())
				{
					var result = Client.FuturesUsdt.GetBrackets(Symbol);

					if(result.Success)
					{
						MaxLeverage = default;

						List<BinanceFuturesSymbolBracket> list = result.Data.ToList();

						foreach(var record in list)
						{
							foreach(var bracket in record.Brackets)
							{
								if(bracket.InitialLeverage > MaxLeverage)
								{
									MaxLeverage = bracket.InitialLeverage;
								}
							}
						}

						return true;
					}
					else
					{
						Logger.Write("GetMaxLeverage: " + result.Error);

						return false;
					}
				}
				else
				{
					Logger.Write("GetMaxLeverage: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("GetMaxLeverage: " + exception.Message);
				
				return false;
			}
		}

		private static bool UpdateMaxLeverage()
		{
			try
			{
				if(IsValid())
				{
					if(MaxLeverage > 0)
					{
						return true;
					}
					else
					{
						bool success = false;

						const int attempts = 3;

						for(int i = 0; i<attempts; ++i)
						{
							success = GetMaxLeverage();

							if(success)
							{
								break;
							}
						}

						return success;
					}
				}
				else
				{
					Logger.Write("UpdateMaxLeverage: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateMaxLeverage: " + exception.Message);

				return false;
			}
		}

		private static bool SetLeverage(int leverage)
		{
			try
			{
				if(IsValid())
				{
					UpdateMaxLeverage();

					if(leverage < 1 || leverage > MaxLeverage)
					{
						Logger.Write("SetLeverage: Invalid Leverage");

						return false;
					}

					var result = Client.FuturesUsdt.ChangeInitialLeverage(Symbol, leverage);

					if(result.Success)
					{
						if(result.Data.Leverage == leverage)
						{
							CurrentLeverage = leverage;

							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						Logger.Write("SetLeverage: " + result.Error);

						return false;
					}
				}
				else
				{
					Logger.Write("SetLeverage: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("SetLeverage: " + exception.Message);

				return false;
			}
		}

		private static bool SetIsolatedTrading()
		{
			try
			{
				if(IsValid())
				{
					var result = Client.FuturesUsdt.ChangeMarginType(Symbol, FuturesMarginType.Isolated);

					if(result.Success)
					{
						return true;
					}
					else
					{
						if(result.Error.Message.Contains("No need to change margin type"))
						{
							return true;
						}
						else
						{
							Logger.Write("SetIsolatedTrading: " + result.Error);

							return false;
						}
					}
				}
				else
				{
					Logger.Write("SetIsolatedTrading: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("SetIsolatedTrading: " + exception.Message);

				return false;
			}
		}
		
		private static bool CheckPosition(string symbol)
		{
			try
			{
				var responce = Client.FuturesUsdt.GetPositionInformation(symbol);

				if(responce.Success)
				{
					IsTrading = true;

					if(responce.Data.ToList().First().IsolatedMargin == 0.0m)
					{
						if(InPosition)
						{
							UpdateFeePrice();

							UpdateBalance();
						}
						
						IsTrading = CancelAllOrders(Symbol);

						InPosition = false;
					}
					else
					{
						InPosition = true;
					}

					return true;
				}
				else
				{
					IsTrading = false;

					Logger.Write("CheckPosition: " + responce.Error.Message);

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("CheckPosition: " + exception.Message);

				return false;
			}
		}

		private static void SetTradeParams()
		{
			try
			{
				Task.Run(() =>
				{
					SetIsolatedTrading();
					
					SetLeverage(DefaultLeverage);
					
					UpdateBalance();
				});
			}
			catch(Exception exception)
			{
				Logger.Write("SetTradeParams: " + exception.Message);
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
	}
}

