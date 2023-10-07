using System;
using System.Threading;
using System.Linq;

using OKX.Api;
using OKX.Api.Enums;

namespace RoverBot
{
	public sealed class Exchange
	{
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public const string SecretKey = "A9D9708CD86F133CCFA6AD8D5998AAC9";

		public const string PassPhrase = "FTY19-641TD-331Eq";
		
		public IBalanceHandler BalanceHandler { get; private set; }

		public IHistoryHandler HistoryHandler { get; private set; }

		public IOrdersHandler OrdersHandler { get; private set; }

		public IPositionHandler PositionHandler { get; private set; }

		public IPriceHandler PriceHandler { get; private set; }

		public ISymbolInfo SymbolInfo { get; private set; }

		public TradingState TradingState { get; private set; }

		public readonly string Symbol;

		private static readonly OKXRestApiClient Client;

		private static readonly OKXWebSocketApiClient Socket;

		private readonly OkxInstrumentType InstrumentType;

		static Exchange()
		{
			try
			{
				Client = new OKXRestApiClient();

				Socket = new OKXWebSocketApiClient();

				Client.SetApiCredentials(ApiKey, SecretKey, PassPhrase);

				Socket.SetApiCredentials(ApiKey, SecretKey, PassPhrase);
			}
			catch(Exception exception)
			{
				Logger.Write("Exchange: " + exception.Message);
			}
		}

		public Exchange(string symbol, OkxInstrumentType instrumentType, int pricePrecision = default)
		{
			try
			{
				if(Client == default)
				{
					Logger.Write("Exchange: Invalid Client");

					return;
				}

				if(Socket == default)
				{
					Logger.Write("Exchange: Invalid Socket");

					return;
				}

				if(symbol == default)
				{
					Logger.Write("Exchange: Invalid Symbol");

					return;
				}

				Symbol = symbol;

				InstrumentType = instrumentType;

				BalanceHandler = new BalanceHandler(Client, Socket);

				WaitForHandlerInitialization(BalanceHandler, TradingState.BalanceHandlerInitialization);

				HistoryHandler = new HistoryHandler(Client, Socket, symbol);

				WaitForHandlerInitialization(HistoryHandler, TradingState.HistoryHandlerInitialization);

				OrdersHandler = new OrdersHandler(Client, Socket, symbol, instrumentType);

				WaitForHandlerInitialization(OrdersHandler, TradingState.OrdersHandlerInitialization);

				PositionHandler = new PositionHandler(Client, Socket, symbol, instrumentType);

				WaitForHandlerInitialization(PositionHandler, TradingState.PositionHandlerInitialization);

				PriceHandler = new PriceHandler(Socket, symbol);

				WaitForHandlerInitialization(PriceHandler, TradingState.PriceHandlerInitialization);

				SymbolInfo = new SymbolInfo(Client, symbol, instrumentType);

				WaitForHandlerInitialization(SymbolInfo, TradingState.SymbolInfoInitialization);

				TradingState = TradingState.InitializationReady;


			}
			catch(Exception exception)
			{
				Logger.Write("Exchange: " + exception.Message);
			}
		}

		private void WaitForHandlerInitialization(IHandlerState handler, TradingState tradingState)
		{
			TradingState = tradingState;

			while(handler.GetHandlerState() == false)
			{
				Thread.Sleep(1);
			}
		}
	}
}
