namespace RoverBot
{
	public interface IPositionHandler : IHandlerState
	{
		bool SetIsolatedMargin();

		bool SetLeverage(int leverage);

		bool ClosePostion();

		decimal GetPosition();
	}
}
