using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.Text.Json;
using System.IO;

using WebSocketSharp;

using Binance.Net;
using Binance.Net.Enums;

using WebSocket = WebSocketSharp.WebSocket;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

using Timer = System.Timers.Timer;
using System.Text;

namespace RoverBot
{
	public static class WebSocketFutures
	{
		private const string Symbol = "BTCUSDT";
		
		public const decimal Percent = 1.0112m;

		private static Timer InternalTimer = default;

		#region CurrentPrice

		private static object LockCurrentPrice = new object();

		public static event Action PriceUpdated = default;

		private static decimal currentPrice = default;

		public static decimal CurrentPrice
		{
			get
			{
				lock(LockCurrentPrice)
				{
					return currentPrice;
				}
			}

			set
			{
				if(value != currentPrice)
				{
					lock(LockCurrentPrice)
					{
						currentPrice = value;
					}

					if(value > 0.0m)
					{
						PriceUpdationTime = DateTime.Now;

						NotifyPropertyChanged(PriceUpdated);
					}
				}
			}
		}

		public static DateTime PriceUpdationTime;

		#endregion

		#region History

		private static object LockHistory = new object();

		public static event Action HistoryUpdated = default;

		private static List<Candle> history = default;

		public static List<Candle> History
		{
			get
			{
				lock(LockHistory)
				{
					return history;
				}
			}

			set
			{
				lock(LockHistory)
				{
					history = value;
				}

				if(CheckHistory())
				{
					HistoryUpdationTime = DateTime.Now;

					NotifyPropertyChanged(HistoryUpdated);
				}
				else
				{
					ResetHistory();
				}
			}
		}

		public static DateTime HistoryUpdationTime;

		public const int HistoryCount = 128;

		private static bool Ready = true;

		#endregion

		static WebSocketFutures()
		{
			try
			{
				StartInternalTimer();

				HistoryUpdated += CheckEntryPoint;
			}
			catch(Exception exception)
			{
				Logger.Write("WebSocketFutures: " + exception.Message);
			}
		}

		public static bool StartPriceStream()
		{
			try
			{
				string symbol = Symbol.ToLower();

				string url = "wss://fstream.binance.com/stream?streams=" + symbol + "@bookTicker";

				WebSocket client = new WebSocket(url);

				client.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

				client.OnMessage += OnPriceUpdated;

				client.OnError += OnPriceSocketError;

				client.OnClose += OnPriceStreamClosed;

				client.Connect();

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("StartPriceStream: " + exception.Message);

				return false;
			}
		}

		private static void OnPriceUpdated(object sender, MessageEventArgs e)
		{
			try
			{
				string str = e.Data;

				BookTickerStream record = JsonSerializer.Deserialize<BookTickerStream>(str);
				
				if(record.Data.GetPrice(out decimal price))
				{
					if(price != CurrentPrice)
					{
						CurrentPrice = price;
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnPriceUpdated: " + exception.Message);
			}
		}

		private static void OnPriceSocketError(object sender, ErrorEventArgs e)
		{
			try
			{
				Logger.Write("OnPriceSocketError: " + e.Message);

				StartPriceStream();
			}
			catch(Exception exception)
			{
				Logger.Write("OnPriceSocketError: " + exception.Message);
			}
		}

		private static void OnPriceStreamClosed(object sender, CloseEventArgs e)
		{
			try
			{
				Logger.Write("PriceStreamClosed");

				StartPriceStream();
			}
			catch(Exception exception)
			{
				Logger.Write("OnPriceStreamClosed: " + exception.Message);
			}
		}

		private static void StartInternalTimer()
		{
			try
			{
				InternalTimer = new Timer();

				InternalTimer.Interval = 1000;

				InternalTimer.Elapsed += InternalTimerElapsed;

				InternalTimer.Start();
			}
			catch(Exception exception)
			{
				Logger.Write("StartInternalTimer: " + exception.Message);
			}
		}

		private static void InternalTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				Task.Run(() =>
				{
					if(Ready)
					{
						Ready = false;

						UpdateHistory();

						Ready = true;
					}
				});
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed: " + exception.Message);
			}
		}

		private static void CheckEntryPoint()
		{
			try
			{
				bool state = true;

				decimal deviation = default;

				decimal quota = default;

				state = state && GetDeviationFactor(History, 120, out deviation);

				state = state && GetQuota(History, 32, out quota);

				Candle.WriteList(History.Last().CloseTime.ToString("HH-mm") + ".txt", History);

				if(state)
				{
					if(deviation >= 1.80m)
					{
						if(quota >= 0.996m)
						{
							decimal takeProfit = Percent * History.Last().Close;

							//BinanceFutures.OnEntryPointDetected(takeProfit);
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

					StringBuilder stringBuilder = new StringBuilder();

					stringBuilder.Append(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
					stringBuilder.Append("\t");

					stringBuilder.Append(Format(deviation, 4));
					stringBuilder.Append("\t");

					stringBuilder.Append(Format(quota, 4));
					stringBuilder.Append("\n");

					File.AppendAllText("Records.txt", stringBuilder.ToString());
				}
				else
				{
					Console.WriteLine("Invalid Model");
				}
			}
			catch(Exception exception)
			{
				Logger.Write("CheckEntryPoint: " + exception.Message);
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

		private static void UpdateHistory()
		{
			try
			{
				if(History == null)
				{
					ResetHistory();

					return;
				}
				
				int count = History.Count;

				if(count != HistoryCount)
				{
					ResetHistory();

					return;
				}

				DateTime startTime = History.First().CloseTime;
				
				DateTime stopTime = History.Last().CloseTime;
				
				DateTime currentTime = DateTime.Now;
				
				TimeSpan timeSpan = currentTime - stopTime;
				
				if(stopTime.AddMinutes(HistoryCount) <= currentTime)
				{
					ResetHistory();
				
					return;
				}
				
				int remind = (int)timeSpan.TotalMinutes;
				
				if(remind > 0)
				{
					List<Candle> list = new List<Candle>();

					if(LoadHistory(Symbol, remind, out var history))
					{
						if(history.Last().CloseTime > History.Last().CloseTime)
						{
							list = list.Concat(History).ToList();

							list = list.Concat(history).ToList();

							list.Reverse();

							list = list.Take(HistoryCount).ToList();

							list.Reverse();

							History = list;
						}
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateHistory: " + exception.Message);

				ResetHistory();
			}
		}

		public static bool CheckHistory()
		{
			try
			{
				if(History == null)
				{
					return false;
				}

				if(History.Count != HistoryCount)
				{
					return false;
				}

				for(int i=default; i<HistoryCount-1; ++i)
				{
					if(History[i+1].CloseTime > History[i].CloseTime.AddSeconds(120.0))
					{
						Logger.Write("CheckHistory: Wrong Sequence");

						return false;
					}
				}

				const int PriceExpiration = 10;

				if(PriceUpdationTime.AddSeconds(PriceExpiration) < DateTime.Now)
				{
					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("CheckHistory: " + exception.Message);

				return false;
			}
		}

		private static void ResetHistory()
		{
			try
			{
				if(LoadHistory(Symbol, HistoryCount, out var history) == false)
				{
					return;
				}

				History = history;
			}
			catch(Exception exception)
			{
				Logger.Write("ResetHistory: " + exception.Message);
			}
		}

		private static bool LoadHistory(string symbol, int count, out List<Candle> history)
		{
			history = default;

			try
			{
				DateTime stopTime = DateTime.Now.AddMinutes(-1).ToUniversalTime();

				DateTime startTime = stopTime.AddMinutes(-count);
				
				bool result = LoadHistory(symbol, startTime, stopTime, out history);

				return result;
			}
			catch(Exception exception)
			{
				Logger.Write("LoadHistory: " + exception.Message);

				return false;
			}
		}

		private static bool LoadHistory(string symbol, DateTime startTime, DateTime stopTime, out List<Candle> history)
		{
			history = new List<Candle>();

			try
			{
				BinanceClient client = new BinanceClient();

				TimeSpan timeSpan = stopTime - startTime;

				int count = (int)Math.Floor(timeSpan.TotalMinutes);

				const int pageSize = 1000;

				const int attempts = 3;

				int pages = count / pageSize;

				int remainder = count - pages*pageSize;

				for(int i=0; i<pages; ++i)
				{
					bool flag = false;

					for(int j=0; j<attempts; ++j)
					{
						var responce = client.FuturesUsdt.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
						if(responce.Success)
						{
							startTime = startTime.AddMinutes(pageSize);
							
							foreach(var record in responce.Data)
							{
								if(record.CloseTime.ToLocalTime() < DateTime.Now)
								{
									history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
								}
							}

							flag = true;

							break;
						}
						else
						{
							Logger.Write("LoadHistory: " + responce.Error.Message);

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
						var responce = client.FuturesUsdt.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
						if(responce.Success)
						{
							startTime = startTime.AddMinutes(pageSize);
							
							foreach(var record in responce.Data)
							{
								if(record.CloseTime.ToLocalTime() < DateTime.Now)
								{
									history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
								}
							}

							flag = true;

							break;
						}
						else
						{
							Logger.Write("LoadHistory: " + responce.Error.Message);

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

		private static void NotifyPropertyChanged(Action eventHandler)
		{
			try
			{
				if(eventHandler != null)
				{
					eventHandler.Invoke();
				}
			}
			catch(Exception exception)
			{
				Logger.Write("NotifyPropertyChanged: " + exception.Message);
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

