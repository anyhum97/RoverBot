using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Binance.Net;
using Binance.Net.Enums;

namespace RoverBot
{
	public static class StrategyBuilder
	{
		private const decimal DefaultBalance = 1000.0m;

		private const int BufferSize = 11280;

		private const int Window = 1200;

		public static bool UpdateStrategy(string symbol, decimal balance, out TradeParams trade)
		{
			trade = default;

			try
			{
				if(LoadStrategy(symbol, out trade))
				{
					return true;
				}

				Logger.Write("UpdateStrategy: Building Model");

				if(balance < TradeBot.MinNotional)
				{
					balance = DefaultBalance;
				}

				if(LoadHistory(symbol, BufferSize, out var history))
				{
					if(ConvertHistory(history, Window, out var bricks))
					{
						if(BuildStrategy(bricks, balance, out trade))
						{
							TradeParams.Append(symbol + ".txt", trade);

							Logger.Write("UpdateStrategy: Ready");

							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateStrategy: " + exception.Message);

				return false;
			}
		}

		private static bool LoadStrategy(string symbol, out TradeParams trade)
		{
			trade = default;

			try
			{
				if(TradeParams.ReadList(symbol + ".txt", out var trades))
				{
					if(trades.Count > 0)
					{
						trades.Sort((x, y) => x.Time.CompareTo(y.Time));

						if(trades.Last().Time.AddHours(4.0) >= DateTime.Now)
						{
							trade = trades.Last();

							return true;
						}
					}
				}

				return false;
			}
			catch(Exception exception)
			{
				Logger.Write("LoadStrategy: " + exception.Message);

				return false;
			}
		}

		private static bool LoadHistory(string symbol, int count, out List<Candle> history)
		{
			history = new List<Candle>();

			try
			{
				BinanceClient client = new BinanceClient();

				DateTime startTime = DateTime.Now.AddMinutes(-count-1).ToUniversalTime();

				const int pageSize = 1000;

				const int attempts = 3;

				int pages = count / pageSize;

				int remainder = count - pages*pageSize;

				for(int i=0; i<pages; ++i)
				{
					bool flag = false;

					for(int j=0; j<attempts; ++j)
					{
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
						if(responce.Success)
						{
							startTime = startTime.AddMinutes(pageSize);
							
							foreach(var record in responce.Data)
							{
								history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
							}

							flag = true;

							break;
						}
						else
						{
							Thread.Sleep(1000);
						}
					}

					if(flag == false)
					{
						return false;
					}
				}

				if(remainder > 0)
				{
					bool flag = false;

					for(int j=0; j<attempts; ++j)
					{
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:remainder);
						
						if(responce.Success)
						{
							foreach(var record in responce.Data)
							{
								history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
							}

							flag = true;

							break;
						}
						else
						{
							Thread.Sleep(1000);
						}
					}

					if(flag == false)
					{
						return false;
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("LoadHistory: " + exception.Message);

				return false;
			}
		}
		
		private static bool ConvertHistory(List<Candle> history, int window, out List<CandleBrick> bricks)
		{
			bricks = default;

			try
			{
				bricks = new List<CandleBrick>();

				for(int i=window; i<history.Count; ++i)
				{
					decimal average = default;

					decimal dispersion = default;

					for(int j=i-window+1; j<=i; ++j)
					{
						average += history[j].Close;
					}

					average = average / window;

					for(int j=i-window+1; j<=i; ++j)
					{
						dispersion += (history[j].Close - average)*(history[j].Close - average);
					}

					decimal deviation = (decimal)Math.Sqrt((double)dispersion / (window - 1));

					bricks.Add(new CandleBrick(history[i].CloseTime, history[i].Open, history[i].Close, history[i].Low, history[i].High, average, deviation));
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("ConvertHistory: " + exception.Message);

				return false;
			}
		}

		private static bool BuildStrategy(List<CandleBrick> history, decimal balance, out TradeParams trade)
		{
			trade = default;

			try
			{
				if(balance < 10.0m)
				{
					return false;
				}

				List<TradeParams> scores = new List<TradeParams>();

				decimal best = default;

				for(decimal factor1 = 1.0m; factor1 <= 6.0m; factor1 = factor1 + 0.1m)
				{
					for(decimal factor2 = 1.0m; factor2 <= 6.0m; factor2 = factor2 + 0.1m)
					{
						for(int stack=10; stack<=100; stack=stack+10)
						{
							if(Evolve(history, balance, factor1, factor2, stack, out decimal total))
							{
								if(total > best)
								{
									scores.Add(new TradeParams(DateTime.Now, factor1, factor2, stack, total));

									best = total;
								}
							}
						}
					}
				}

				scores.Sort((x, y) => y.Result.CompareTo(x.Result));

				decimal value1 = default;

				decimal value2 = default;

				int value3 = default;
				
				for(int i=0; i<Math.Min(scores.Count, 4); ++i)
				{
					decimal start1 = scores[i].Factor1 - 0.1m;

					decimal stop1 = scores[i].Factor1 + 0.1m;

					decimal start2 = scores[i].Factor2 - 0.1m;

					decimal stop2 = scores[i].Factor2 + 0.1m;

					int start3 = scores[i].Stack - 10;

					int stop3 = scores[i].Stack + 10;

					for(decimal factor1 = start1; factor1 <= stop1; factor1 = factor1 + 0.01m)
					{
						for(decimal factor2 = start2; factor2 <= stop2; factor2 = factor2 + 0.01m)
						{
							for(int stack=start3; stack<=stop3; stack=stack+1)
							{
								if(Evolve(history, balance, factor1, factor2, stack, out decimal total))
								{
									if(total >= best)
									{
										value1 = factor1;

										value2 = factor2;

										value3 = stack;

										best = total;
									}
								}
							}
						}
					}
				}

				trade = new TradeParams(DateTime.Now, value1, value2, value3, best);

				if(best > 0.0m)
				{
					return true;
				}
				else
				{
					Logger.Write("BuildStrategy: Can Not Find Profitable Strategy");

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("StrategyBuilder.BuildStrategy: " + exception.Message);

				return false;
			}
		}

		private static bool Evolve(List<CandleBrick> history, decimal balance, decimal factor1, decimal factor2, int stack, out decimal total)
		{
			total = default;

			try
			{
				var list = new List<Tuple<decimal, decimal, decimal>>();
				
				const decimal fee = 1.0m-0.00075m;

				decimal start = balance;

				for(int i=0; i<history.Count; ++i)
				{
					decimal price = history[i].Close;
				
					decimal high = history[i].High;

					if(Buy(history, i, balance, factor1, factor2, stack, out int operations, out decimal sellPrice))
					{
						decimal notional = 10.0m*operations;

						if(balance >= notional)
						{
							decimal volume = fee*notional/price;
						
							list.Add(new Tuple<decimal, decimal, decimal>(volume, price, sellPrice));
						
							balance = balance - notional;
						}
					}
					else
					{
						var buffer = new List<Tuple<decimal, decimal, decimal>>();

						foreach(var record in list)
						{
							if(high >= record.Item3)
							{
								balance += fee*record.Item1*high;
							}
							else
							{
								buffer.Add(record);
							}
						}

						list = buffer;
					}
				}
				
				decimal frozen = default;

				foreach(var record in list)
				{
					frozen += fee*record.Item1*history.Last().Close;
				}

				total = balance + frozen - start;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("Evolve: " + exception.Message);

				return false;
			}
		}

		private static bool Buy(List<CandleBrick> history, int index, decimal balance, decimal factor1, decimal factor2, int stack, out int operations, out decimal sellPrice)
		{
			operations = default;

			sellPrice = default;

			try
			{
				decimal price = history[index].Close;

				decimal average = history[index].Average;

				decimal deviation = history[index].Deviation;

				if(price < average)
				{
					decimal delta = average - price;

					decimal ratio = delta / deviation;

					if(ratio >= factor1)
					{
						sellPrice = price + factor2*deviation;

						for(int x=stack; x>=1; --x)
						{
							if(balance >= x*10.0m)
							{
								operations = x;

								return true;
							}
						}
					}

					return false;
				}
				else
				{
					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("Buy: " + exception.Message);

				return false;
			}
		}
	}
}

