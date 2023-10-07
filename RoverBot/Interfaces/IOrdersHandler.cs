using System.Collections.Generic;

namespace RoverBot
{
	public interface IOrdersHandler : IHandlerState
	{
		bool PlaceLongMarketOrder(decimal volume);

		bool PlaceShortMarketOrder(decimal volume);

		bool PlaceLongBestPriceOrder(decimal price, decimal volume);

		bool PlaceShortBestPriceOrder(decimal price, decimal volume);

		bool PlaceLongLimitOrder(decimal price, decimal volume);

		bool PlaceShortLimitOrder(decimal price, decimal volume);

		bool PlaceLongTakeProfitMarketOrder(decimal price, decimal volume);

		bool PlaceLongStopLossMarketOrder(decimal price, decimal volume);

		bool PlaceShortTakeProfitMarketOrder(decimal price, decimal volume);

		bool PlaceShortStopLossMarketOrder(decimal price, decimal volume);

		bool CancelAllOrders();

		List<Order> GetOrdersList();
	}
}
