namespace RoverBot
{
	public interface ISymbolInfo : IHandlerState
	{
		decimal GetMinLot();

		decimal GetPriceTick();

		int GetPricePrecision();

		int GetMinLeverage();

		int GetMaxLeverage();
	}
}
