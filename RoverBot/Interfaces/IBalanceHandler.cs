namespace RoverBot
{
	public interface IBalanceHandler : IHandlerState
	{
		decimal GetTotalBalance();

		decimal GetAvailableBalance();

		decimal GetFrozenBalance();
	}
}
