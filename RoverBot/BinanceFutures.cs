using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;

using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;

using Timer = System.Timers.Timer;

namespace RoverBot
{
	public static partial class BinanceFutures
	{
		public const string ApiKey = "2SZQJS9YCll34RsYJ0S8BtUqXwtL8CrG6cFzM5A4iXa2toaLxjAr3vn8r44sFPM5";

		public const string SecretKey = "NFxPBdmA0KrlvbX3Sk2BgYWenjAEZ3zgwxeEOG0e6NWtHHzxsQdgPdZDOMSFjrQ7";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.923";

		public static string Symbol = Currency2 + Currency1;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.001m;

		public const int DefaultLeverage = 6;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 3;

		private static BinanceClient Client = default;

		private static Timer InternalTimer1 = default;

		private static Timer InternalTimer2 = default;

		public static bool IsValid()
		{
			try
			{
				if(State == default)
				{
					return false;
				}

				if(Client == default)
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
		
		public static void RestartRoverBot()
		{
			try
			{
				const string FileName = "RestartRoverBot.exe";

				if(File.Exists(FileName))
				{
					Logger.Write("RoverBot: Restarting...");

					Process process = new Process();

					process.StartInfo.FileName = FileName;

					process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

					process.StartInfo.UseShellExecute = false;

					process.Start();
				}
				else
				{
					Logger.Write("RestartRoverBot: Invalid File");
				}
			}
			catch(Exception exception)
			{
				Logger.Write("RestartRoverBot: " + exception.Message);
			}
		}

		private static void StartInternalTimer1()
		{
			try
			{
				InternalTimer1 = new Timer();

				InternalTimer1.Interval = 30000;

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

				InternalTimer2.Interval = 1800000;

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
					CheckPositionAsync(Symbol).Wait();

					UpdateBalanceAsync().Wait();
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

		public static void OnEntryPointDetected(decimal price, decimal takeProfit)
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
								const decimal depositFactor = 0.99m;

								decimal deposit = depositFactor * Balance;

								int deals = (int)Math.Floor(deposit * CurrentLeverage / (price * VolumeFilter));

								decimal volume = deals * VolumeFilter;

								Logger.Write("OnEntryPointDetected: [Entry Point]");

								PlaceLongOrderAsync(Symbol, volume, price, takeProfit).Wait();

								CheckPositionAsync(Symbol).Wait();

								UpdateBalanceAsync().Wait();
							}
							else
							{
								Logger.Write("OnEntryPointDetected: Invalid Leverage");
							}
						}
						else
						{
							Logger.Write("OnEntryPointDetected: Already In Position");
						}
					}
					else
					{
						Logger.Write("OnEntryPointDetected: [Entry Point]");
					}
				}
				else
				{
					Logger.Write("OnEntryPointDetected: [Entry Point]");
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnEntryPointDetected: " + exception.Message);
			}
		}

		private static void SetTradeParams()
		{
			try
			{
				Task.Run(() =>
				{
					SetIsolatedTradingAsync().Wait();
					
					SetLeverageAsync(DefaultLeverage).Wait();
					
					UpdateBalanceAsync().Wait();
				});
			}
			catch(Exception exception)
			{
				Logger.Write("SetTradeParams: " + exception.Message);
			}
		}
		
		public static async Task<bool> StartRoverBotAsync()
		{
			try
			{
				Logger.Write(string.Format("RoverBot({0}, {1}) Version {2} Started", Symbol, DefaultLeverage, Version));

				Logger.Write(ApiKey);

				Client = new BinanceClient();
				
				Client.SetApiCredentials(ApiKey, SecretKey);
				
				WebSocketFutures.StartKlineStream();
				
				while(IsTrading == false)
				{
					if(await CheckPositionAsync(Symbol))
					{
						break;
					}
					
					Thread.Sleep(1000);
				}
				
				State = true;
				
				UpdateBalanceAsync().Wait();

				StartInternalTimer1();

				StartInternalTimer2();
				
				SetTradeParams();

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("BinanceFutures: " + exception.Message);

				Client = default;

				State = default;

				return false;
			}
		}

		public static async Task<bool> PlaceLongOrderAsync(string symbol, decimal volume, decimal price, decimal takeProfit1)
		{
			try
			{
				if(IsValid())
				{
					if(price > 0.0m)
					{
						if(symbol == default)
						{
							return false;
						}
						
						volume = Math.Round(volume, VolumePrecision);
						
						if(volume < VolumeFilter)
						{
							return false;
						}

						takeProfit1 = Math.Round(takeProfit1, PricePrecision);
						
						if(takeProfit1 <= price)
						{
							return false;
						}

						const decimal percent = 1.01m;

						decimal takeProfit2 = percent * takeProfit1;

						takeProfit2 = Math.Round(takeProfit2, PricePrecision);

						if(takeProfit2 <= price)
						{
							return false;
						}

						const decimal border = 10.0m;

						var orders = new BinanceFuturesBatchOrder[3];

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
							Type = OrderType.TakeProfit,
							PositionSide = PositionSide.Both,
							TimeInForce = TimeInForce.GoodTillCancel,
							StopPrice = takeProfit1 - border,
							Price = takeProfit1,
							Quantity = volume,
							ReduceOnly = true,
						};

						orders[2] = new BinanceFuturesBatchOrder()
						{
							Symbol = symbol,
							Side = OrderSide.Sell,
							Type = OrderType.TakeProfit,
							PositionSide = PositionSide.Both,
							TimeInForce = TimeInForce.GoodTillCancel,
							StopPrice = takeProfit2 - border,
							Price = takeProfit2,
							Quantity = volume,
							ReduceOnly = true,
						};

						var responce = await Client.FuturesUsdt.Order.PlaceMultipleOrdersAsync(orders);
						
						try
						{
							var data = responce.Data.ToArray();
							
							for(int i=default; i<data.Length; ++i)
							{
								if(data[i].Success == false)
								{
									Logger.Write(string.Format("PlaceLongOrder[{0}]: {1}", i, data[i].Error.Message));
								}
							}
						}
						catch(Exception exception)
						{
							Logger.Write("PlaceLongOrder: " + exception.Message);
						}

						if(responce.Success)
						{
							decimal orderPrice = responce.Data.First().Data.AvgPrice;

							Logger.Write("PlaceLongOrder: HistoryPrice = " + Format(price, PricePrecision));

							Logger.Write("PlaceLongOrder: OrderPrice = " + Format(orderPrice, PricePrecision));

							Logger.Write("PlaceLongOrder: TakeProfit = " + Format(takeProfit1, PricePrecision));

							Logger.Write("PlaceLongOrder: Volume = " + Format(volume, VolumePrecision));

							return true;
						}
						else
						{
							Logger.Write("PlaceLongOrder: " + responce.Error.Message);
							
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

		private static async Task<bool> CancelAllOrdersAsync(string symbol)
		{
			try
			{
				const int attempts = 3;

				for(int i=default; i<attempts; ++i)
				{
					var responce = await Client.FuturesUsdt.Order.CancelAllOrdersAsync(symbol);

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

		private static async Task<bool> UpdateBalanceAsync()
		{
			try
			{
				if(IsValid())
				{
					var responce = await Client.FuturesUsdt.Account.GetBalanceAsync();

					if(responce.Success)
					{
						var list = responce.Data.ToList();

						for(int i=default; i<list.Count; ++i)
						{
							if(list[i].Asset == Currency1)
							{
								if(TotalBalance != LastBalance)
								{
									LastBalance = TotalBalance;
								}

								Balance = list[i].CrossWalletBalance;

								TotalBalance = list[i].AvailableBalance;

								Frozen = TotalBalance - Balance;

								return true;
							}

							if(list[i].Asset == "BNB")
							{
								FeeCoins = list[i].AvailableBalance;

								if(FeePrice == default)
								{
									UpdateFeePriceAsync().Wait();
								}

								FeeBalance = FeePrice * FeeCoins;
							}
						}

						return false;
					}
					else
					{
						Logger.Write("UpdateBalance: " + responce.Error.Message);

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

		private static async Task<bool> UpdateFeePriceAsync()
		{
			try
			{
				var responce = await Client.Spot.Market.GetPriceAsync("BNBUSDT");

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

		private static async Task<bool> SetIsolatedTradingAsync()
		{
			try
			{
				if(IsValid())
				{
					var responce = await Client.FuturesUsdt.ChangeMarginTypeAsync(Symbol, FuturesMarginType.Isolated);

					if(responce)
					{
						return true;
					}
					else
					{
						if(responce.Error.Message.Contains("No need to change margin type"))
						{
							return true;
						}
						else
						{
							Logger.Write("SetIsolatedTrading: " + responce.Error.Message);

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

		private static async Task<bool> SetLeverageAsync(int leverage)
		{
			try
			{
				if(IsValid())
				{
					var responce = await Client.FuturesUsdt.ChangeInitialLeverageAsync(Symbol, leverage);

					if(responce.Success)
					{
						if(responce.Data.Leverage == leverage)
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
						Logger.Write("SetLeverage: " + responce.Error.Message);

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
		
		private static async Task<bool> CheckPositionAsync(string symbol)
		{
			try
			{
				var responce = await Client.FuturesUsdt.GetPositionInformationAsync(symbol);

				if(responce.Success)
				{
					IsTrading = true;

					if(responce.Data.ToList().First().IsolatedMargin == default)
					{
						if(InPosition)
						{
							UpdateFeePriceAsync().Wait();

							UpdateBalanceAsync().Wait();
						}
						
						IsTrading = await CancelAllOrdersAsync(Symbol);

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
					IsTrading = default;

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

