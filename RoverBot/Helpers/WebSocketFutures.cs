using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Globalization;
using System.Threading;
using System.Text.Json;
using System.Text;
using System.IO;

using WebSocketSharp;

using Binance.Net;
using Binance.Net.Enums;

using WebSocket = WebSocketSharp.WebSocket;

using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace RoverBot
{
	public static class WebSocketFutures
	{
		private const string Symbol = "BTCUSDT";
		
		public const decimal Percent = 1.013m;

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

		public static DateTime PriceServerTime;

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
			}
		}

		public static DateTime HistoryUpdationTime;

		public static DateTime LastKlineUpdated;

		public const int HistoryCount = 180;

		#endregion

		static WebSocketFutures()
		{
			try
			{
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

		private static void OnPriceUpdated(object sender, MessageEventArgs e)
		{
			try
			{
				string str = e.Data;

				BookTickerStream record = JsonSerializer.Deserialize<BookTickerStream>(str);
				
				if(record.Data.GetPrice(out decimal price))
				{
					if(record.Data.GetTime(out DateTime time))
					{
						CurrentPrice = price;

						PriceServerTime = time;
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnPriceUpdated: " + exception.Message);
			}
		}
		
		public static bool StartKlineStream()
		{
			try
			{
				string symbol = Symbol.ToLower();

				string url = "wss://fstream.binance.com/stream?streams=" + symbol + "@kline_1m";

				WebSocket client = new WebSocket(url);

				client.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

				client.OnMessage += OnKlineUpdated;

				client.OnError += OnKlineSocketError;

				client.OnClose += OnKlineStreamClosed;

				client.Connect();

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
				Logger.Write("OnKlineStreamClosed");

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

						Thread.Sleep(4000);

						if(LoadHistory(Symbol, HistoryCount, out var history))
						{
							History = history;
						}
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("OnKlineUpdated: " + exception.Message);
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
					if(deviation >= 1.892m)
					{
						if(quota >= 0.996m)
						{
							decimal takeProfit = Percent * History.Last().Close;
							
							BinanceFutures.OnEntryPointDetected(takeProfit);
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

					WriteRecord(deviation, quota);
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
				
				stringBuilder.Append(History.Last().CloseTime.ToString("dd.MM.yyyy HH:mm"));
				stringBuilder.Append("\t");
				
				stringBuilder.Append(Format(deviation, 4));
				stringBuilder.Append("\t");
				
				stringBuilder.Append(Format(quota, 4));
				stringBuilder.Append("\n");
				
				File.AppendAllText("Records.txt", stringBuilder.ToString());
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
				if(History == null)
				{
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

				const double PriceExpiration = 10.0;

				if(PriceServerTime > History.Last().CloseTime.AddSeconds(PriceExpiration))
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

