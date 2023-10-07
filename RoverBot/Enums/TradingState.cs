namespace RoverBot
{
	public enum TradingState
	{
		Invalid = default,
		InvalidHandlerState,
		BalanceHandlerInitialization,
		HistoryHandlerInitialization,
		OrdersHandlerInitialization,
		PositionHandlerInitialization,
		PriceHandlerInitialization,
		SymbolInfoInitialization,
		InitializationReady,
		CheckLeverageDone,
		SetTradingParamsDone,
		Trading,
	}
}
