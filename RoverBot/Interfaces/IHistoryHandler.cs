using System.Collections.Generic;

namespace RoverBot
{
	public interface IHistoryHandler
	{
		List<Kline> GetHistory();

		decimal GetCurrentPrice();

		bool GetHandlerState();
	}
}
