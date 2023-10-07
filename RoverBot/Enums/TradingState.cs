namespace RoverBot
{
	public enum TradingState
	{
		Invalid = default,

		BalanceHandlerInitialization,
		HistoryHandlerInitialization,
		OrdersHandlerInitialization,
		PositionHandlerInitialization,
		PriceHandlerInitialization,
		SymbolInfoInitialization,
		InitializationReady,

	}
}
