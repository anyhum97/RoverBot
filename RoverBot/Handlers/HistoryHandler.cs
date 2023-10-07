using System;
using System.Collections.Generic;
using System.Linq;

using OKX.Api;
using OKX.Api.Models.MarketData;
using OKX.Api.Enums;

namespace RoverBot
{
	public sealed class HistoryHandler : IHistoryHandler
	{
		public const int HistoryExpirationTime = 80000;

		public const int MinHistoryCount = 1;

		public const int MaxHistoryCount = 299;

		public List<Kline> History { get; private set; }

		public bool IsAvailable { get; private set; }

		public readonly string Symbol;

		public readonly OkxPeriod Period;

		public readonly int HistoryCount;

		private readonly OKXRestApiClient Client;

		private readonly OKXWebSocketApiClient Socket;

		private DateTime LastUpdationTime;

		private DateTime LastKlineServerTime;

		public HistoryHandler(OKXRestApiClient client, OKXWebSocketApiClient socket, string symbol, int historyCount = MaxHistoryCount, OkxPeriod period = OkxPeriod.OneMinute)
		{
			if(client == default)
			{
				throw new Exception();
			}

			if(socket == default)
			{
				throw new Exception();
			}

			if(symbol == default)
			{
				throw new Exception();
			}

			if(historyCount < MinHistoryCount || historyCount > MaxHistoryCount)
			{
				throw new Exception();
			}

			Client = client;

			Socket = socket;

			Symbol = symbol;

			Period = period;

			HistoryCount = historyCount;

			IsAvailable = UpdateHistory();

			Socket.OrderBookTrading.MarketData.SubscribeToCandlesticksAsync(SocketUpdation, symbol, period);
		}

		private void SocketUpdation(OkxCandlestick data)
		{
			try
			{
				if(data.Time != LastKlineServerTime)
				{
					LastKlineServerTime = data.Time;

					IsAvailable = UpdateHistory();
				}
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("HistoryHandler.SocketUpdation({0}): {1}", Symbol, exception.Message));
			}
		}

		public bool UpdateHistory()
		{
			try
			{
				var history = Client.OrderBookTrading.MarketData.GetCandlesticksAsync(Symbol, Period, limit: HistoryCount + 1).Result;

				if(history.Success == false)
				{
					Logger.Write(string.Format("HistoryHandler.UpdateHistory({0}): {1}", Symbol, history.Error.Message));

					return false;
				}

				History = history.Data.Select(x => new Kline(x.Time.AddMinutes(1.0).ToLocalTime(), x.Open, x.Close, x.Low, x.High)).ToList().GetRange(1, HistoryCount);

				LastKlineServerTime = history.Data.First().Time;

				LastUpdationTime = DateTime.Now;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("HistoryHandler.UpdateHistory({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public List<Kline> GetHistory()
		{
			return History;
		}

		public int GetMaxHistoryCount()
		{
			return MaxHistoryCount;
		}

		public bool GetHandlerState()
		{
			if(LastUpdationTime.AddMilliseconds(HistoryExpirationTime) < DateTime.Now)
			{
				IsAvailable = false;
			}

			return IsAvailable;
		}
	}
}
