using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Text;
using System.IO;

using WebSocketSharp;

using Binance.Net;
using Binance.Net.Enums;

using WebSocket = WebSocketSharp.WebSocket;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

using Timer = System.Timers.Timer;

namespace RoverBot
{
	public static class WebSocketFutures
	{
		private const string Symbol = "BTCUSDT";
		
		public const decimal Percent = 1.013m;

		private static object LockRecordFile = new object();

		private static WebSocket KlineStream = default;

		private static Timer InternalTimer = default;

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
					NotifyPropertyChanged(HistoryUpdated);
				}
			}
		}

		public static DateTime LastKlineUpdated;

		public const int HistoryCount = 180;

		#endregion
		
		#region StartUpdationTime

		private static object LockStartUpdationTime = new object();

		private static DateTime startUpdationTime;

		public static DateTime StartUpdationTime
		{
			get
			{
				lock(LockStartUpdationTime)
				{
					return startUpdationTime;
				}
			}

			set
			{
				lock(LockStartUpdationTime)
				{
					startUpdationTime = value;
				}
			}
		}

		#endregion

		#region StopUpdationTime

		private static object LockStopUpdationTime = new object();

		private static DateTime stopUpdationTime;

		public static DateTime StopUpdationTime
		{
			get
			{
				lock(LockStopUpdationTime)
				{
					return stopUpdationTime;
				}
			}

			set
			{
				lock(LockStopUpdationTime)
				{
					stopUpdationTime = value;
				}
			}
		}

		#endregion

		static WebSocketFutures()
		{
			try
			{
				HistoryUpdated += CheckEntryPoint;

				StartInternalTimer();
			}
			catch(Exception exception)
			{
				Logger.Write("WebSocketFutures: " + exception.Message);
			}
		}

		private static void StartInternalTimer()
		{
			try
			{
				InternalTimer = new Timer();

				InternalTimer.Interval = 30000;

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
				CheckKlineStream();
			}
			catch(Exception exception)
			{
				Logger.Write("InternalTimerElapsed: " + exception.Message);
			}
		}

		public static bool StartKlineStream()
		{
			try
			{
				string symbol = Symbol.ToLower();

				string url = "wss://fstream.binance.com/stream?streams=" + symbol + "@kline_1m";

				KlineStream = new WebSocket(url);

				KlineStream.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

				KlineStream.OnMessage += OnKlineUpdated;

				KlineStream.OnError += OnKlineSocketError;

				KlineStream.OnClose += OnKlineStreamClosed;

				KlineStream.Connect();

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("StartKlineStream: " + exception.Message);

				return false;
			}
		}

		private static void OnKlineSocketError(object sender, ErrorEventArgs e)
		{
			try
			{
				Logger.Write("OnKlineSocketError: " + e.Message);

				StartKlineStream();
			}
			catch(Exception exception)
			{
				Logger.Write("OnKlineSocketError: " + exception.Message);
			}
		}

		private static void OnKlineStreamClosed(object sender, CloseEventArgs e)
		{
			try
			{
				Logger.Write("Kline Stream Closed");

				StartKlineStream();
			}
			catch(Exception exception)
			{
				Logger.Write("OnKlineStreamClosed: " + exception.Message);
			}
		}

		private static void OnKlineUpdated(object sender, MessageEventArgs e)
		{
			try
			{
				string str = e.Data;

				KlineTickerStream record = JsonSerializer.Deserialize<KlineTickerStream>(str);

				if(record.Data.GetTime(out DateTime time))
				{
					if(time != LastKlineUpdated)
					{
						LastKlineUpdated = time;

						Task.Run(() =>
						{
							Thread.Sleep(4000);

							StartUpdationTime = DateTime.Now;

							if(LoadHistory(Symbol, HistoryCount, out var history))
							{
								History = history;
							}
						});
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnKlineUpdated: " + exception.Message);
			}
		}
		
		private static void CheckKlineStream()
		{
			try
			{
				if(KlineStream == default)
				{
					Logger.Write("CheckKlineStream: Invalid Stream");

					StartKlineStream();

					return;
				}

				if(KlineStream.IsAlive == default)
				{
					Logger.Write("CheckKlineStream: Kline Stream Closed");

					StartKlineStream();

					return;
				}

				const double KlineStreamExpiration = 120.0;

				if(StartUpdationTime.AddSeconds(KlineStreamExpiration) <= DateTime.Now)
				{
					Logger.Write("CheckKlineStream: Kline Updation Failed [1]");

					StartKlineStream();

					return;
				}

				if(StopUpdationTime.AddSeconds(KlineStreamExpiration) <= DateTime.Now)
				{
					Logger.Write("CheckKlineStream: Kline Updation Failed [2]");

					StartKlineStream();

					return;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("CheckKlineStream: " + exception.Message);
			}
		}

		private static void CheckEntryPoint()
		{
			try
			{
				bool state = true;

				decimal deviation = default;

				decimal quota = default;

				state = state && GetDeviationFactor(History, 140, out deviation);

				state = state && GetQuota(History, 28, out quota);
				
				if(state)
				{
					Task.Run(() =>
					{
						WriteRecord(deviation, quota);
					});

					if(deviation >= 1.892m)
					{
						if(quota >= 0.996m)
						{
							Task.Run(() =>
							{
								decimal price = History.Last().Close;

								decimal takeProfit = Percent * price;

								BinanceFutures.OnEntryPointDetected(price, takeProfit);
							});
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
			catch(Exception exception)
			{
				Logger.Write("CheckEntryPoint: " + exception.Message);
			}
		}

		private static void WriteRecord(decimal deviation, decimal quota)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				
				TimeSpan timeSpan = StopUpdationTime - StartUpdationTime;

				string time = History.Last().CloseTime.ToString("dd.MM.yyyy HH:mm");

				string updationTime = string.Format("[{0}]", Format(timeSpan.TotalSeconds, 4));

				stringBuilder.Append(time);
				stringBuilder.Append("\t");
				
				stringBuilder.Append(Format(deviation, 4));
				stringBuilder.Append("\t");
				
				stringBuilder.Append(Format(quota, 4));
				stringBuilder.Append("\t");
				
				stringBuilder.Append(updationTime);
				stringBuilder.Append("\n");

				lock(LockRecordFile)
				{
					File.AppendAllText("Records.txt", stringBuilder.ToString());
				}
			}
			catch(Exception exception)
			{
				Logger.Write("WriteRecord: " + exception.Message);
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

		private static bool LoadHistory(string symbol, int count, out List<Candle> history)
		{
			history = new List<Candle>();

			try
			{
				BinanceClient client = new BinanceClient();

				var responce = client.FuturesUsdt.Market.GetKlines(symbol, KlineInterval.OneMinute, limit: count);
				
				if(responce.Success)
				{
					foreach(var record in responce.Data)
					{
						if(record.CloseTime.ToLocalTime() < LastKlineUpdated)
						{
							history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
						}
					}
					
					return true;
				}
				else
				{
					Logger.Write("LoadHistory: " + responce.Error.Message);

					return false;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("LoadHistory: " + exception.Message);

				return false;
			}
		}

		private static bool CheckHistory()
		{
			try
			{
				if(History == default)
				{
					Logger.Write("CheckHistory: Invalid Buffer");

					return false;
				}

				const int SizeBorder = 160;

				if(History.Count < SizeBorder)
				{
					Logger.Write("CheckHistory: Invalid Sequence");

					return false;
				}

				const double HistoryExpiration = 60.0;

				for(int i=default; i<History.Count-1; ++i)
				{
					if(History[i+1].CloseTime > History[i].CloseTime.AddSeconds(HistoryExpiration).AddMilliseconds(1.0))
					{
						Logger.Write("CheckHistory: Wrong Sequence");

						return false;
					}
				}

				StopUpdationTime = DateTime.Now;

				TimeSpan timeSpan = StopUpdationTime - StartUpdationTime;

				const double HistoryExpired = 6.0;

				if(timeSpan.TotalSeconds >= HistoryExpired)
				{
					Logger.Write("CheckHistory: History Expired");

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

		private static string Format(double value, int sign = 4)
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

