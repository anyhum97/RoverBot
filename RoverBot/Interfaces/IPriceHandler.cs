namespace RoverBot
{
	public interface IPriceHandler : IHandlerState
	{
		decimal GetAskPrice();

		decimal GetBidPrice();
	}
}
