using System.Collections.Generic;

namespace RoverBot
{
	public interface IOrdersHandler : IHandlerState
	{
		bool PlaceLongMarketOrder(decimal volume);

		bool PlaceShortMarketOrder(decimal volume);

		bool PlaceLongBestPriceOrder(decimal price, decimal volume);

		bool PlaceShortBestPriceOrder(decimal price, decimal volume);

		bool PlaceLongLimitOrder(decimal price, decimal volume, out long orderId);

		bool PlaceShortLimitOrder(decimal price, decimal volume, out long orderId);

		bool PlaceLongTakeProfitMarketOrder(decimal price, decimal volume, out long orderId);

		bool PlaceLongStopLossMarketOrder(decimal price, decimal volume, out long orderId);

		bool PlaceShortTakeProfitMarketOrder(decimal price, decimal volume, out long orderId);

		bool PlaceShortStopLossMarketOrder(decimal price, decimal volume, out long orderId);

		public bool CancelOrder(long orderId);

		bool CancelAllOrders();

		List<Order> GetOrdersList();
	}
}
