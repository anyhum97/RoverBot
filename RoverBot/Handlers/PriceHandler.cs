using System;

using OKX.Api;
using OKX.Api.Models.MarketData;

namespace RoverBot
{
	public sealed class PriceHandler : IPriceHandler
	{
		public readonly string Symbol;

		public decimal AskPrice { get; private set; }

		public decimal BidPrice { get; private set; }

		public bool IsAvailable { get; private set; }

		private readonly OKXWebSocketApiClient Socket;

		public PriceHandler(OKXWebSocketApiClient socket, string symbol)
		{
			if(socket == default)
			{
				throw new Exception();
			}

			if(symbol == default)
			{
				throw new Exception();
			}

			Socket = socket;

			Symbol = symbol;

			Socket.OrderBookTrading.MarketData.SubscribeToTickersAsync(SocketUpdation, symbol);
		}

		private void SocketUpdation(OkxTicker data)
		{
			try
			{
				AskPrice = data.AskPrice;
				
				BidPrice = data.BidPrice;

				IsAvailable = true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("BalanceHandler.SocketUpdation({0}): {1}", Symbol, exception.Message));
			}
		}

		public decimal GetAskPrice()
		{
			return AskPrice;
		}
		
		public decimal GetBidPrice()
		{
			return BidPrice;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
