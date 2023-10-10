using System.Collections.Generic;

namespace RoverBot
{
	public interface IHistoryHandler : IHandlerState
	{
		List<Kline> GetHistory();

		public Kline GetCurrentKline();

		int GetMaxHistoryCount();
	}
}
