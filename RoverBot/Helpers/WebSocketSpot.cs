using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;

using WebSocketSharp;

using Binance.Net;
using Binance.Net.Enums;

using WebSocket = WebSocketSharp.WebSocket;

using Timer = System.Timers.Timer;

namespace RoverBot
{
	public static class WebSocketSpot
	{
		private static string Symbol = TradeBot.Symbol;

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

		public const int HistoryCount = 2048;

		private static bool Ready = true;

		#endregion

		static WebSocketSpot()
		{
			try
			{
				StartInternalTimer();
			}
			catch(Exception exception)
			{
				Logger.Write("WebSocketSpot: " + exception.Message);
			}
		}

		public static bool StartPriceStream()
		{
			try
			{
				string symbol = Symbol.ToLower();

				string url = "wss://stream.binance.com/stream?streams=" + symbol + "@bookTicker";

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

				bool flag = false;

				for(int i=0; i<HistoryCount-1; ++i)
				{
					if(History[i+1].CloseTime != History[i].CloseTime.AddMinutes(1.0))
					{
						if(flag)
						{
							Logger.Write("CheckHistory: Wrong Sequence");

							return false;
						}

						flag = true;
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
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
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
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
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
	}
}

