namespace RoverBot
{
	public interface IBalanceHandler
	{
		decimal GetTotalBalance();

		decimal GetAvailableBalance();

		decimal GetFrozenBalance();

		bool GetHandlerState();
	}
}
