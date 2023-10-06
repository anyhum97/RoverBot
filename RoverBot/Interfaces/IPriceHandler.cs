namespace RoverBot
{
	public interface IPriceHandler
	{
		decimal GetAskPrice();

		decimal GetBidPrice();

		bool GetHandlerState();
	}
}
