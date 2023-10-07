namespace RoverBot
{
	public interface IExchange : IHandlerState
	{
		IBalanceHandler BalanceHandler { get; }

		IHistoryHandler HistoryHandler { get; }
	}
}
