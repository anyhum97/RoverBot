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

		public const string ApiKey = "LF6LgLNhFZcMPRkasacEsmc7fQJ4qRydCVakhf99V76IIH4cMER1QNTSLHa2aPqt";

		public const string SecretKey = "7OMVQjWx7IVqmQ33Uuc01o4X8DNMUdUO22EwkGj0q70KVjjr2xV45WivqaTYohDq";

		public const string Currency1 = "USDT";

		public const string Currency2 = "BTC";

		public const string Version = "0.7741";

		public static string Symbol = Currency2 + Currency1;
		
		public const decimal ReduceStack = 1.0m;

		public const decimal MinNotional = 10.0m;

		public const decimal PriceFilter = 0.01m;

		public const decimal VolumeFilter = 0.000001m;
		
		public const decimal StepSize = 0.000001m;

		public const decimal Percent = 1.0127m;

		public const int CurrencyPrecision1 = 2;

		public const int CurrencyPrecision2 = 6;

		public const int CurrencyPrecision3 = 6;

		public const int NotionalPrecision = 4;

		public const int PricePrecision = 2;

		public const int VolumePrecision = 6;

		public static List<SellOrder> SellOrders { get; private set; } = default;

		public static decimal Balance1 { get; private set; } = default;

		public static decimal Balance2 { get; private set; } = default;

		public static decimal TotalBalance { get; private set; } = default;

		public static decimal FeeCoins { get; private set; } = default;

		public static decimal FeePrice { get; private set; } = default;

		public static bool IsTrading { get; set; } = default;

		private static BinanceClient Client = default;

		private static Timer InternalTimer1 = default;

		private static Timer InternalTimer2 = default;

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
							IsTrading = true;

							break;
						}
					}
				}

				WebSocketSpot.HistoryUpdated += OnHistoryUpdated;

				StartInternalTimer1();

				StartInternalTimer2();

				CheckSellOrders();
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

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("IsValid: " + exception.Message);

				return false;
			}
		}

		private static bool UpdateBalance()
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

								stringBuilder.Append(Format(Balance1, CurrencyPrecision1));
								stringBuilder.Append(" ");
								stringBuilder.Append(Currency1);
								stringBuilder.Append(", ");

								stringBuilder.Append(Format(Balance2, CurrencyPrecision2));
								stringBuilder.Append(" ");
								stringBuilder.Append(Currency2);

								stringBuilder.Append(", ");
								stringBuilder.Append(Format(FeeCoins, CurrencyPrecision3));
								stringBuilder.Append(" BNB");

								stringBuilder.Append(", Total = ");
								stringBuilder.Append(Format(TotalBalance, CurrencyPrecision1));
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

		private static bool UpdateСurrencyInfo()
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
						
						Logger.Write("UpdateСurrencyInfo: Invalid Symbol");

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

				InternalTimer2.Interval = 60000;

				InternalTimer2.Elapsed += InternalTimerElapsed2;

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
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed2: " + exception.Message);
			}
		}

		private static void OnHistoryUpdated()
		{
			try
			{
				if(IsValid())
				{
					bool state = true;

					decimal deviation = default;

					decimal quota = default;

					state = state && GetDeviationFactor(WebSocketSpot.History, 120, out deviation);

					state = state && GetQuota(WebSocketSpot.History, 32, out quota);

					if(state)
					{
						if(deviation >= 1.70m)
						{
							if(quota >= 0.996m)
							{
								if(Balance1 >= MinNotional + ReduceStack)
								{
									decimal buyPrice = WebSocketSpot.CurrentPrice;

									decimal sellPrice = Percent * WebSocketSpot.History.Last().Close;

									Buy(buyPrice, sellPrice);
								}
								else
								{
									Console.WriteLine("Entry Point");
								}
							}
							else
							{
								Console.WriteLine("Skip");
							}
						}
						else
						{
							Console.WriteLine("Skip");
						}
					}
					else
					{
						Console.WriteLine("Invalid Model");
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnHistoryUpdated: " + exception.Message);
			}
		}

		private static bool GetAverage(List<Candle> history, int window, out decimal average)
		{
			average = default;

			try
			{
				int index = history.Count-3;

				for(int i=index-window+1; i<index; ++i)
				{
					decimal price = history[i].Close;

					average += price;
				}

				average /= window;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetAverage: " + exception.Message);

				return false;
			}
		}

		private static bool GetDeviation(List<Candle> history, int window, out decimal average, out decimal deviation)
		{
			average = default;

			deviation = default;

			try
			{
				int index = history.Count-3;

				if(GetAverage(history, window, out average) == false)
				{
					return false;
				}
				
				deviation = default;

				for(int i=index-window+1; i<index; ++i)
				{
					decimal price = history[i].Close;

					deviation += (price - average) * (price - average);
				}

				deviation = (decimal)Math.Sqrt((double)deviation / (window - 1));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetDeviation: " + exception.Message);

				return false;
			}
		}

		private static bool GetDeviationFactor(List<Candle> history, int window, out decimal factor)
		{
			factor = default;

			try
			{
				int index = history.Count-3;

				if(GetDeviation(history, window, out decimal average, out decimal deviation) == false)
				{
					return false;
				}

				decimal delta = average - history[index].Close;

				factor = delta / deviation;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetDeviationFactor: " + exception.Message);

				return false;
			}
		}

		private static bool GetQuota(List<Candle> history, int window, out decimal quota)
		{
			quota = default;

			try
			{
				decimal more = default;

				decimal less = default;

				int index = history.Count-1;

				decimal price2 = history[index].Close;

				for(int i=index-window+1; i<index; ++i)
				{
					decimal price1 = history[i].Close;

					decimal delta = Math.Abs(price1 - price2);

					if(price1 > price2)
					{
						more += delta;
					}
					else
					{
						less += delta;
					}
				}

				if(more + less != default)
				{
					quota = more / (more + less);
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetQuota: " + exception.Message);

				return false;
			}
		}

		public static bool Buy(decimal buyPrice, decimal sellPrice)
		{
			try
			{
				if(IsValid())
				{
					Logger.Write(CheckLine);

					Logger.Write("Buy: Balance = " + Format(Balance1, 2));

					if(Balance1 < MinNotional + ReduceStack)
					{
						Logger.Write("Buy: Invalid Balance");
						
						Logger.Write(CheckLine);

						return false;
					}
					
					if(buyPrice <= 0.0m)
					{
						Logger.Write("Buy: BuyPrice <= 0.0");
						
						Logger.Write(CheckLine);

						return false;
					}
					
					if(sellPrice <= buyPrice)
					{
						Logger.Write("Buy: SellPrice <= BuyPrice");
						
						Logger.Write(CheckLine);

						return false;
					}

					decimal sellFactor = (sellPrice-buyPrice)/buyPrice;

					decimal volume = (Balance1-ReduceStack)/buyPrice;
					
					decimal notional = default;

					if(PlaceBuyOrder(ref volume, ref buyPrice, ref notional, out long buyId))
					{
						const int attempts = 3;

						for(int i=0; i<attempts; ++i)
						{
							if(PlaceSellOrder(ref volume, ref sellPrice, ref notional, out long sellOrderId))
							{
								StringBuilder stringBuilder = new StringBuilder();

								stringBuilder.Append("Я купил ");
								stringBuilder.Append(Format(volume, VolumePrecision));
								stringBuilder.Append(" монеты ");
								stringBuilder.Append(Currency2);
								stringBuilder.Append(" по цене ");
								stringBuilder.Append(Format(buyPrice, PricePrecision));
								stringBuilder.Append(" и установил наценку в ");
								stringBuilder.Append(Format(100.0m*sellFactor, 2));
								stringBuilder.Append("%");
								
								TelegramBot.Send(stringBuilder.ToString());

								Logger.Write("BuyStack: Success");

								Logger.Write(CheckLine);

								UpdateFeePrice();

								UpdateBalance();

								return true;
							}
						}
						
						StringBuilder sellOrderError = new StringBuilder();

						sellOrderError.Append("Я купил ");
						sellOrderError.Append(Format(volume, VolumePrecision));
						sellOrderError.Append(" монеты ");
						sellOrderError.Append(Currency2);
						sellOrderError.Append(" по цене ");
						sellOrderError.Append(Format(buyPrice, PricePrecision));
						sellOrderError.Append(", но мне не удалось выставить ордер на продажу.\n\n");
						sellOrderError.Append("Цена продажи: ");

						sellOrderError.Append(Format(sellPrice, PricePrecision));

						TelegramBot.Send(sellOrderError.ToString());

						Logger.Write("BuyStack: Invalid Sell Order");

						Logger.Write(CheckLine);

						UpdateFeePrice();

						UpdateBalance();

						return false;
					}
					else
					{
						Logger.Write("BuyStack: Can Not Place Buy Order");
						
						Logger.Write(CheckLine);

						return false;
					}
				}
				else
				{
					Logger.Write("BuyStack: Invalid Account");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("BuyStack: " + exception.Message);
				
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
						WebCallResult<BinancePlacedOrder> request = Client.Spot.Order.PlaceOrder(Symbol, OrderSide.Buy, OrderType.Market, volume);

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
				inputParams.Append(Format(volume, VolumePrecision));
				inputParams.Append(", Price = ");
				inputParams.Append(Format(price, PricePrecision));
				inputParams.Append(", Notional = ");
				inputParams.Append(Format(price*volume, NotionalPrecision));
				
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
				outputParams.Append(Format(notional, NotionalPrecision));
				
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
						bool update = default;

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

											update = true;
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
						
						if(update == false)
						{
							UpdateFeePrice();
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

