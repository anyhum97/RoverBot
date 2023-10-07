using System;

namespace RoverBot
{
	public abstract class BaseExchange : IHandlerState
	{
		public IBalanceHandler BalanceHandler { get; private set; }

		public IHistoryHandler HistoryHandler { get; private set; }

		public IOrdersHandler OrdersHandler { get; private set; }

		public IPositionHandler PositionHandler { get; private set; }

		public IPriceHandler PriceHandler { get; private set; }

		public ISymbolInfo SymbolInfo { get; private set; }

		public virtual void Trade()
		{

		}

		public abstract bool GetHandlerState();
	}
}
