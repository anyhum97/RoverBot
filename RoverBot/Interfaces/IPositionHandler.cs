namespace RoverBot
{
	public interface IPositionHandler : IHandlerState
	{
		bool SetIsolatedMargin();

		bool SetLeverage(int leverage);

		decimal GetPosition();
	}
}
