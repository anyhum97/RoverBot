using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Timers;
using System.Text;
using System.Linq;

using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.SpotData;

using CryptoExchange.Net.Objects;

using Timer = System.Timers.Timer;

namespace RoverBot
{
	public static class TradeBot
	{
		public const string CheckLine = "******************************************************************************";

		public const string ApiKey = "RuqJRpQ62CErjxJp7OTwShtiFFFS8mdsSPOTgtKXFWUr6lBeqk0T0UA4B463pttL";

		public const string SecretKey = "BrBD2UJbYdxUxCBsRFGqV9zK6HLZHQjbHtKEoME7LByXYgGzlrD7oHsf9zWUGkLL";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.447";

		public static string Symbol = Currency2 + Currency1;
		
		public const decimal MinNotional = 10.00m;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.000001m;
		
		public const decimal StepSize = 0.000001m;

		public const int NotionalPrecision = 4;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 6;
		
		public static List<SellOrder> SellOrders { get; private set; } = default;

		public static decimal Balance1 { get; private set; } = default;

		public static decimal Balance2 { get; private set; } = default;

		public static decimal TotalBalance { get; private set; } = default;

		public static decimal Frozen { get; private set; } = default;

		public static decimal FeeCoins { get; private set; } = default;

		public static decimal FeePrice { get; private set; } = default;

		public static bool IsTrading { get; set; } = default;

		private static DateTime LastRatioPrinted = default;

		private static BinanceClient Client = default;

		private static TradeParams Trade = default;

		private static Timer InternalTimer1 = default;

		private static Timer InternalTimer2 = default;

		private static decimal Average = default;

		private static decimal Deviation = default;

		private static bool HistoryLock = default;

		private static bool Ready = true;

		public static void Start()
		{
			try
			{
				Logger.Write(CheckLine);

				Logger.Write("RoverBot Version " + Version + " Started");

				Client = new BinanceClient();

				Client.SetApiCredentials(ApiKey, SecretKey);
				
				SellOrders = new List<SellOrder>();
				
				WebSocketSpot.StartPriceStream();

				TelegramBot.Start();

				while(true)
				{
					if(UpdateFeePrice())
					{
						if(UpdateBalance())
						{
							if(StrategyBuilder.UpdateStrategy(Symbol, TotalBalance, out var trade))
							{
								IsTrading = true;

								Trade = trade;

								break;
							}
						}
					}
				}

				WebSocketSpot.HistoryUpdated += OnHistoryUpdated;

				WebSocketSpot.PriceUpdated += OnPriceUpdated;

				StartInternalTimer1();

				StartInternalTimer2();
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

				var response = Client.General.GetAccountInfo();

				if(response.Success)
				{
					bool find1 = default;

					bool find2 = default;

					bool find3 = default;

					bool updated1 = default;

					bool updated2 = default;

					bool updated3 = default;

					decimal total1 = default;

					decimal total2 = default;

					foreach(var record in response.Data.Balances)
					{
						if(find1 == false)
						{
							if(record.Asset.Contains(Currency1))
							{
								decimal balance1 = record.Free;

								if(balance1 != Balance1)
								{
									Balance1 = balance1;
									
									updated1 = true;
								}

								total1 = record.Total;

								find1 = true;
							}
						}

						if(find2 == false)
						{
							if(record.Asset.Contains(Currency2))
							{
								decimal balance2 = record.Free;

								if(balance2 != Balance2)
								{
									Balance2 = balance2;
									
									updated2 = true;
								}

								total2 = record.Total;

								find2 = true;
							}
						}

						if(find3 == false)
						{
							if(record.Asset.Contains("BNB"))
							{
								decimal balance3 = record.Free;

								if(balance3 != FeeCoins)
								{
									FeeCoins = balance3;
									
									updated3 = true;
								}
								
								find3 = true;
							}
						}

						if(find1 && find2 && find3)
						{
							TotalBalance = total1 + total2*price + FeeCoins*FeePrice;

							if(updated1 || updated2 || updated3)
							{
								StringBuilder stringBuilder = new StringBuilder();

								stringBuilder.Append("UpdateBalance: ");

								stringBuilder.Append(Format(Balance1, 4));
								stringBuilder.Append(" ");
								stringBuilder.Append(Currency1);
								stringBuilder.Append(", ");

								stringBuilder.Append(Format(Balance2, 6));
								stringBuilder.Append(" ");
								stringBuilder.Append(Currency2);

								stringBuilder.Append(", ");
								stringBuilder.Append(Format(FeeCoins, 4));
								stringBuilder.Append(" BNB");

								stringBuilder.Append(", Total = ");
								stringBuilder.Append(Format(TotalBalance, 2));
								stringBuilder.Append(" USDT");

								if(FeeCoins < 0.01m)
								{
									TelegramBot.Send("Малый остаток BNB монет");
								}

								Logger.Write(stringBuilder.ToString());
							}

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

		public static bool UpdateСurrencyInfo()
		{
			if(IsValid())
			{
				try
				{
					var request = Client.Spot.System.GetExchangeInfo();

					if(request.Success)
					{
						foreach(var record in request.Data.Symbols)
						{
							if(record.Name.Contains(Symbol))
							{
								return true;
							}
						}
						
						Logger.Write("UpdateСurrencyInfo: Can Not Find Symbol");

						return false;
					}
					else
					{
						Logger.Write("UpdateСurrencyInfo: Bad Request, Error = " + request.Error.Message);

						return false;
					}
				}
				catch(Exception exception)
				{
					Logger.Write("UpdateСurrencyInfo: " + exception.Message);

					return false;
				}
			}
			else
			{
				Logger.Write("UpdateСurrencyInfo: Invalid Account");

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

				InternalTimer2.Interval = 3600000;

				InternalTimer2.Elapsed += InternalTimerElapsed1;

				InternalTimer2.Start();
			}
			catch(Exception exception)
			{
				Logger.Write("StartInternalTimer2: " + exception.Message);
			}
		}

		private static void InternalTimerElapsed1(object sender, ElapsedEventArgs e)
		{
			try
			{
				Task.Run(() =>
				{
					UpdateBalance();

					CheckSellOrders();
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed1: " + exception.Message);
			}
		}

		private static void InternalTimerElapsed2(object sender, ElapsedEventArgs e)
		{
			try
			{
				Task.Run(() =>
				{
					UpdateFeePrice();

					if(StrategyBuilder.UpdateStrategy(Symbol, TotalBalance, out var trade))
					{
						Trade = trade;
					}
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed2: " + exception.Message);
			}
		}

		public static bool UpdateStandartDeviation()
		{
			try
			{
				List<decimal> list = new List<decimal>();

				foreach(var record in WebSocketSpot.History)
				{
					list.Add(record.Close);
				}

				Average = list.Average();

				decimal value = default;

				foreach(var record in list)
				{
					value += (Average - record)*(Average - record);
				}

				Deviation = (decimal)Math.Sqrt((double)value / (list.Count - 1));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateStandartDeviation: " + exception.Message);

				return false;
			}
		}

		private static void OnHistoryUpdated()
		{
			try
			{
				if(IsValid())
				{
					if(UpdateStandartDeviation())
					{
						HistoryLock = default;
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnHistoryUpdated: " + exception.Message);
			}
		}

		private static void OnPriceUpdated()
		{
			try
			{
				if(IsValid())
				{
					if(Average == default || Deviation == default)
					{
						return;
					}

					decimal price = 1.0006m*WebSocketSpot.CurrentPrice;

					if(price < Average)
					{
						decimal delta = Average - price;

						decimal ratio = delta / Deviation;

						if(LastRatioPrinted.AddSeconds(1.0) <= DateTime.Now)
						{
							Console.WriteLine(Format(ratio, 4));

							LastRatioPrinted = DateTime.Now;
						}
						
						if(ratio >= Trade.Factor1)
						{
							Logger.Write("Entry Point on Price " + Format(price, PricePrecision) + " Detected (Ratio: " + ratio + ")");
						}

						if(ratio >= Trade.Factor1)
						{
							decimal sellPrice = price + Trade.Factor2 * Deviation;

							if(IsTrading && Ready)
							{
								Ready = false;

								for(int x=Trade.Stack; x>=1; --x)
								{
									if(Balance1 >= x*10.0m)
									{
										if(HistoryLock == false)
										{
											if(Buy(x, price, sellPrice))
											{
												HistoryLock = true;

												break;
											}
										}
									}
								}

								Ready = true;
							}
						}
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnPriceUpdated: " + exception.Message);
			}
		}

		public static bool Buy(decimal stack, decimal price, decimal sellPrice)
		{
			try
			{
				if(IsValid())
				{
					Logger.Write(CheckLine);

					Logger.Write("Buy: Stack = " + Format(stack, 2));

					if(stack < 1.0m)
					{
						Logger.Write("Buy: Stack < 1.0");
						
						Logger.Write(CheckLine);

						return false;
					}
					
					if(price <= 0.0m)
					{
						Logger.Write("Buy: Price <= 0.0");
						
						Logger.Write(CheckLine);

						return false;
					}
					
					if(sellPrice <= price)
					{
						Logger.Write("Buy: SellPrice <= Price");
						
						Logger.Write(CheckLine);

						return false;
					}

					decimal sellFactor = (sellPrice-price)/price;

					decimal volume = MinNotional*stack/price;
					
					decimal notional = default;

					if(PlaceBuyOrder(ref volume, ref price, ref notional, out long buyId))
					{
						const int attempts = 3;

						for(int i=0; i<attempts; ++i)
						{
							if(PlaceSellOrder(ref volume, ref sellPrice, ref notional, out long sellOrderId))
							{
								StringBuilder stringBuilder = new StringBuilder();

								stringBuilder.Append("Я купил ");
								stringBuilder.Append(Format(volume, 2));
								stringBuilder.Append(" монеты ");
								stringBuilder.Append(Currency2);
								stringBuilder.Append(" по цене ");
								stringBuilder.Append(Format(price, 4));
								stringBuilder.Append(" и установил наценку в ");
								stringBuilder.Append(Format(100.0m*(sellFactor - 1.0m), 2));
								stringBuilder.Append("%");
								
								TelegramBot.Send(stringBuilder.ToString());

								Logger.Write("Buy: Success");

								Logger.Write(CheckLine);

								UpdateFeePrice();

								UpdateBalance();

								return true;
							}
						}
						
						StringBuilder sellOrderError = new StringBuilder();

						sellOrderError.Append("Я купил ");
						sellOrderError.Append(Format(volume, 2));
						sellOrderError.Append(" монеты ");
						sellOrderError.Append(Currency2);
						sellOrderError.Append(" по цене ");
						sellOrderError.Append(Format(price, 4));
						sellOrderError.Append(", но мне не удалось выставить ордер на продажу.\n\n");
						sellOrderError.Append("Цена продажи: ");

						sellOrderError.Append(Format(sellPrice, 4));

						TelegramBot.Send(sellOrderError.ToString());

						Logger.Write("Buy: Can Not Place Sell Order");

						Logger.Write(CheckLine);

						UpdateFeePrice();

						UpdateBalance();

						return false;
					}
					else
					{
						Logger.Write("Buy: Can Not Place Buy Order");
						
						Logger.Write(CheckLine);

						return false;
					}
				}
				else
				{
					Logger.Write("Buy: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("Buy: " + exception.Message);
				
				return false;
			}
		}

		private static bool PlaceBuyOrder(ref decimal volume, ref decimal price, ref decimal notional, out long orderId)
		{
			orderId = default;

			try
			{
				if(IsValid())
				{
					if(ValidateTradeParams(ref volume, ref price, ref notional))
					{
						WebCallResult<BinancePlacedOrder> request = Client.Spot.Order.PlaceOrder(Symbol, OrderSide.Buy, OrderType.Limit, volume, price:price, timeInForce:TimeInForce.FillOrKill);

						if(request.Success)
						{
							orderId = request.Data.OrderId;

							StringBuilder stringBuilder = new StringBuilder();

							stringBuilder.Append("PlaceBuyOrder: Volume = ");
							stringBuilder.Append(Point(volume));

							stringBuilder.Append(", Price = ");
							stringBuilder.Append(Point(price));

							stringBuilder.Append(", Notional = ");
							stringBuilder.Append(Format(notional, NotionalPrecision));

							stringBuilder.Append(", Id = ");
							stringBuilder.Append(orderId);

							Logger.Write(stringBuilder.ToString());

							if(request.Data.Status == OrderStatus.Filled)
							{
								Logger.Write("PlaceBuyOrder: Filled");

								return true;
							}
							else
							{
								Logger.Write("PlaceBuyOrder: " + request.Data.Status);

								return false;
							}
						}
						else
						{
							StringBuilder errorLog = new StringBuilder();

							errorLog.Append("PlaceBuyOrder: Volume = ");
							errorLog.Append(Point(volume));
							errorLog.Append(" " + Currency2);
							
							errorLog.Append(", Price = ");
							errorLog.Append(Point(price));
							errorLog.Append(" " + Currency1);
							
							errorLog.Append(", Notional = ");
							errorLog.Append(Point(notional));

							errorLog.Append(", Error = ");

							errorLog.Append(request.Error.Message);

							Logger.Write(errorLog.ToString());

							return false;
						}
					}
					else
					{
						Logger.Write("PlaceBuyOrder: Invalid Order");

						return false;
					}
				}
				else
				{
					Logger.Write("PlaceBuyOrder: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				StringBuilder exceptionLog = new StringBuilder();
				
				exceptionLog.Append("PlaceBuyOrder: Volume = ");
				exceptionLog.Append(Point(volume));
				exceptionLog.Append(" " + Currency2);
				
				exceptionLog.Append(", Price = ");
				exceptionLog.Append(Point(price));
				exceptionLog.Append(" " + Currency1);
				
				exceptionLog.Append(", Exception = ");
				
				exceptionLog.Append(exception.Message);
				
				Logger.Write(exceptionLog.ToString());
				
				return false;
			}
		}

		private static bool PlaceSellOrder(ref decimal volume, ref decimal price, ref decimal notional, out long orderId)
		{
			orderId = default;

			try
			{
				if(IsValid())
				{
					if(ValidateTradeParams(ref volume, ref price, ref notional))
					{
						WebCallResult<BinancePlacedOrder> request = Client.Spot.Order.PlaceOrder(Symbol, OrderSide.Sell, OrderType.Limit, volume, price:price, timeInForce:TimeInForce.GoodTillCancel);

						if(request.Success)
						{
							orderId = request.Data.OrderId;

							StringBuilder stringBuilder = new StringBuilder();

							stringBuilder.Append("PlaceSellOrder: Volume = ");
							stringBuilder.Append(Point(volume));

							stringBuilder.Append(", Price = ");
							stringBuilder.Append(Point(price));

							stringBuilder.Append(", Notional = ");
							stringBuilder.Append(Format(notional, NotionalPrecision));

							stringBuilder.Append(", Id = ");
							stringBuilder.Append(orderId);

							Logger.Write(stringBuilder.ToString());

							if(request.Data.Status == OrderStatus.New)
							{
								Logger.Write("PlaceSellOrder: Placed");

								return true;
							}

							if(request.Data.Status == OrderStatus.Filled)
							{
								Logger.Write("PlaceSellOrder: Filled");

								return true;
							}

							Logger.Write("PlaceSellOrder: " + request.Data.Status);

							return false;
						}
						else
						{
							StringBuilder errorLog = new StringBuilder();

							errorLog.Append("PlaceSellOrder: Volume = ");
							errorLog.Append(Point(volume));
							errorLog.Append(" " + Currency2);
							
							errorLog.Append(", Price = ");
							errorLog.Append(Point(price));
							errorLog.Append(" " + Currency1);
							
							errorLog.Append(", Notional = ");
							errorLog.Append(Point(notional));

							errorLog.Append(", Error = ");
							errorLog.Append(request.Error.Message);

							Logger.Write(errorLog.ToString());

							return false;
						}
					}
					else
					{
						Logger.Write("PlaceSellOrder: Invalid Order");

						return false;
					}
				}
				else
				{
					Logger.Write("PlaceSellOrder: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				StringBuilder exceptionLog = new StringBuilder();
				
				exceptionLog.Append("PlaceSellOrder: Volume = ");
				exceptionLog.Append(Point(volume));
				exceptionLog.Append(" " + Currency2);
				
				exceptionLog.Append(", Price = ");
				exceptionLog.Append(Point(price));
				exceptionLog.Append(" " + Currency1);
				
				exceptionLog.Append(", Exception = ");
				exceptionLog.Append(exception.Message);
				
				Logger.Write(exceptionLog.ToString());
				
				return false;
			}
		}

		private static bool ValidateTradeParams(ref decimal volume, ref decimal price, ref decimal notional)
		{
			try
			{
				StringBuilder inputParams = new StringBuilder();
				
				inputParams.Append("ValidateTradeParams: Volume = ");
				inputParams.Append(Format(volume));
				inputParams.Append(", Price = ");
				inputParams.Append(Format(price));
				inputParams.Append(", Notional = ");
				inputParams.Append(Format(price*volume));
				
				Logger.Write(inputParams.ToString());
				
				if(volume <= 0.0m)
				{
					Logger.Write("ValidateTradeParams: Volume <= 0.0");
					
					return false;
				}
				
				if(price <= 0.0m)
				{
					Logger.Write("ValidateTradeParams: Price <= 0.0");
				
					return false;
				}
				
				if(PriceFilter == 0.0m)
				{
					Logger.Write("ValidateTradeParams: PriceFilter == 0.0");
					
					return false;
				}
				
				if(StepSize == 0.0m)
				{
					Logger.Write("ValidateTradeParams: StepSize == 0.0");
					
					return false;
				}
				
				if(PricePrecision < 0 || PricePrecision > 8)
				{
					Logger.Write("ValidateTradeParams: Invalid PricePrecision");
					
					return false;
				}
				
				if(VolumePrecision < 0 || VolumePrecision > 8)
				{
					Logger.Write("ValidateTradeParams: Invalid VolumePrecision");
					
					return false;
				}
				
				if(NotionalPrecision < 0 || NotionalPrecision > 8)
				{
					Logger.Write("ValidateTradeParams: Invalid NotionalPrecision");
				}
				
				if(price % PriceFilter != 0.0m)
				{
					price = price - price % PriceFilter + PriceFilter;
				}
				
				price = Math.Round(price, PricePrecision);
				
				if(volume % StepSize != 0.0m)
				{
					volume = volume - volume % StepSize + StepSize;
				}
				
				volume = Math.Round(volume, VolumePrecision);
				
				notional = price*volume;
				
				if(notional < 0.99m*MinNotional)
				{
					StringBuilder tradeParams = new StringBuilder();
					
					tradeParams.Append("ValidateTradeParams: Volume = ");
					tradeParams.Append(Point(volume));
					tradeParams.Append(", Price = ");
					tradeParams.Append(Point(price));
					tradeParams.Append(" Notional = ");
					tradeParams.Append(Point(notional));
					
					Logger.Write(tradeParams.ToString());
					
					return false;
				}
				
				if(notional < MinNotional)
				{
					volume = (MinNotional+StepSize) / price;
				}
				
				if(volume % StepSize != 0.0m)
				{
					volume = volume - volume % StepSize + StepSize;
				}
				
				volume = Math.Round(volume, VolumePrecision);
				
				StringBuilder outputParams = new StringBuilder();
				
				outputParams.Append("ValidateTradeParams: Volume = ");
				outputParams.Append(Point(volume));
				outputParams.Append(", Price = ");
				outputParams.Append(Point(price));
				outputParams.Append(" Notional = ");
				outputParams.Append(Point(notional));
				
				Logger.Write(outputParams.ToString());
				
				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("ValidateTradeParams: " + exception.Message);

				return false;
			}
		}
		
		private static bool CheckSellOrders()
		{
			try
			{
				if(IsValid())
				{
					var response = Client.Spot.Order.GetAllOrders(Symbol, startTime:DateTime.Now.AddMonths(-1).ToUniversalTime());

					if(response.Success)
					{
						foreach(var order in response.Data)
						{
							if(order.Side == OrderSide.Sell)
							{
								int index = default;
								
								bool find = default;

								for(int i=0; i<SellOrders.Count; ++i)
								{
									if(SellOrders[i].OrderId == order.OrderId)
									{
										find = true;

										index = i;

										break;
									}
								}

								if(order.Status == OrderStatus.New)
								{
									if(find == false)
									{
										SellOrders.Add(new SellOrder(order.OrderId, order.Quantity, order.Price));
									}
								}
								else
								{
									if(find)
									{
										if(order.Status == OrderStatus.Filled)
										{
											Logger.Write("Sell Order #" + order.OrderId + " Was Filled");

											TelegramBot.Send(SellOrders[index].Filled());
										}
										else
										{
											Logger.Write("Sell Order #" + order.OrderId + " Was " + order.Status);
										}

										SellOrders.RemoveAt(index);
									}
								}
							}
						}
						
						return true;
					}
					else
					{
						Logger.Write("CheckSellOrders: " + response.Error.Message);

						return false;
					}
				}
				else
				{
					Logger.Write("CheckSellOrders: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("CheckSellOrders: " + exception.Message);
				
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

