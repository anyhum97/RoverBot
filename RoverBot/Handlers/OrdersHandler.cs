using System;
using System.Collections.Generic;
using System.Timers;

using OKX.Api;
using OKX.Api.Models.Trade;
using OKX.Api.Models.AlgoTrading;
using OKX.Api.Enums;

namespace RoverBot
{
	public sealed class OrdersHandler : IOrdersHandler
	{
		public const int TimerElapsedTime = 40000;

		public List<Order> Orders { get; private set; }

		public bool IsAvailable { get; private set; }

		public readonly string Symbol;

		private readonly OKXRestApiClient Client;

		private readonly OKXWebSocketApiClient Socket;

		private readonly object LockOrdersList = new object();

		private readonly OkxInstrumentType InstrumentType;

		private readonly Timer MainTimer;

		public OrdersHandler(OKXRestApiClient client, OKXWebSocketApiClient socket, string symbol, OkxInstrumentType instrumentType = OkxInstrumentType.Swap)
		{
			if(client == default)
			{
				throw new Exception();
			}

			if(socket == default)
			{
				throw new Exception();
			}

			if(symbol == default)
			{
				throw new Exception();
			}

			Client = client;

			Socket = socket;

			Symbol = symbol;

			InstrumentType = instrumentType;

			IsAvailable = UpdateOrdersList();

			Socket.OrderBookTrading.Trade.SubscribeToOrderUpdatesAsync(SocketUpdation, instrumentType, instrumentId: symbol);

			Socket.OrderBookTrading.AlgoTrading.SubscribeToAlgoOrderUpdatesAsync(AlgoSocketUpdation, instrumentType, instrumentId: symbol);

			MainTimer = new Timer(TimerElapsedTime);

			MainTimer.Elapsed += TimerElapsed;

			MainTimer.Start();
		}

		private void SocketUpdation(OkxOrder data)
		{
			try
			{
				IsAvailable = UpdateOrdersList();
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.SocketUpdation({0}): {1}", Symbol, exception.Message));
			}
		}

		private void AlgoSocketUpdation(OkxAlgoOrder data)
		{
			try
			{
				IsAvailable = UpdateOrdersList();
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.AlgoSocketUpdation({0}): {1}", Symbol, exception.Message));
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				IsAvailable = UpdateOrdersList();
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.TimerElapsed({0}): {1}", Symbol, exception.Message));
			}
		}

		private bool UpdateOrdersList()
		{
			try
			{
				var orders = Client.OrderBookTrading.Trade.GetOrderListAsync(InstrumentType, Symbol, state: OkxOrderState.Live).Result;

				var algo = Client.OrderBookTrading.AlgoTrading.GetAlgoOrderListAsync(OkxAlgoOrderType.Conditional, instrumentId: Symbol).Result;

				lock(LockOrdersList)
				{
					Orders = new List<Order>();

					foreach(var record in orders.Data)
					{
						var type = OrderTypeExtensions.GetOrderType(record);

						decimal price = default;

						if(record.Price.HasValue)
						{
							price = record.Price.Value;
						}

						Orders.Add(new Order(type, price, record.Quantity.Value, record.OrderId));
					}

					foreach(var record in algo.Data)
					{
						var type = OrderTypeExtensions.GetOrderType(record);

						decimal price = default;

						if(type == OrderType.LongTakeProfitMarketOrder || type == OrderType.ShortTakeProfitMarketOrder)
						{
							price = record.TakeProfitTriggerPrice.Value;
						}

						if(type == OrderType.LongStopLossMarketOrder || type == OrderType.ShortStopLossMarketOrder)
						{
							price = record.StopLossTriggerPrice.Value;
						}

						Orders.Add(new Order(type, price, record.Quantity.Value, record.OrderId));
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.UpdateOrdersList({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceLongMarketOrder(decimal volume)
		{
			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Buy, OkxPositionSide.Net, OkxOrderType.MarketOrder, volume).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				Logger.Write(string.Format("OrdersHandler.PlaceLongMarketOrder({0}): Volume = {1} [OK]", Symbol, volume));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceLongMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceShortMarketOrder(decimal volume)
		{
			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.MarketOrder, volume).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				Logger.Write(string.Format("OrdersHandler.PlaceShortMarketOrder({0}): Volume = {1} [OK]", Symbol, volume));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceShortMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}
		
		public bool PlaceLongBestPriceOrder(decimal price, decimal volume)
		{
			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Buy, OkxPositionSide.Net, OkxOrderType.OptimalLimitOrder, volume).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongBestPriceOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongBestPriceOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				Logger.Write(string.Format("OrdersHandler.PlaceLongBestPriceOrder({0}): Volume = {1} [OK]", Symbol, volume));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceLongBestPriceOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceShortBestPriceOrder(decimal price, decimal volume)
		{
			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.OptimalLimitOrder, volume).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortBestPriceOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortBestPriceOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				Logger.Write(string.Format("OrdersHandler.PlaceShortBestPriceOrder({0}): Volume = {1} [OK]", Symbol, volume));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceShortBestPriceOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceLongLimitOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Buy, OkxPositionSide.Net, OkxOrderType.LimitOrder, volume, price).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongLimitOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongLimitOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.OrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceLongLimitOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.OrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceLongLimitOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceShortLimitOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.Trade.PlaceOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxPositionSide.Net, OkxOrderType.LimitOrder, volume, price).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortLimitOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortLimitOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.OrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceShortLimitOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.OrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceShortLimitOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceLongTakeProfitMarketOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.AlgoTrading.PlaceAlgoOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxAlgoOrderType.Conditional, null, OkxPositionSide.Net, volume, tpTriggerPrice: price, tpTriggerPriceType: OkxAlgoPriceType.Last, tpOrderPrice: -1, reduceOnly: true).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongTakeProfitMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongTakeProfitMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.AlgoOrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceLongTakeProfitMarketOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.AlgoOrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceLongTakeProfitMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceLongStopLossMarketOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.AlgoTrading.PlaceAlgoOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Sell, OkxAlgoOrderType.Conditional, null, OkxPositionSide.Net, volume, slTriggerPrice: price, slTriggerPriceType: OkxAlgoPriceType.Last, slOrderPrice: -1, reduceOnly: true).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongStopLossMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceLongStopLossMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.AlgoOrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceLongStopLossMarketOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.AlgoOrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceLongStopLossMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceShortTakeProfitMarketOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.AlgoTrading.PlaceAlgoOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Buy, OkxAlgoOrderType.Conditional, null, OkxPositionSide.Net, volume, tpTriggerPrice: price, tpTriggerPriceType: OkxAlgoPriceType.Last, tpOrderPrice: -1, reduceOnly: true).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortTakeProfitMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortTakeProfitMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.AlgoOrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceShortTakeProfitMarketOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.AlgoOrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceShortTakeProfitMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool PlaceShortStopLossMarketOrder(decimal price, decimal volume, out long orderId)
		{
			orderId = default;

			try
			{
				var order = Client.OrderBookTrading.AlgoTrading.PlaceAlgoOrderAsync(Symbol, OkxTradeMode.Isolated, OkxOrderSide.Buy, OkxAlgoOrderType.Conditional, null, OkxPositionSide.Net, volume, slTriggerPrice: price, slTriggerPriceType: OkxAlgoPriceType.Last, slOrderPrice: -1, reduceOnly: true).Result;

				if(order.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortStopLossMarketOrder({0}): {1}", Symbol, order.Error.Message));

					return false;
				}

				if(order.Data.ErrorCode != default && order.Data.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.PlaceShortStopLossMarketOrder({0}): {1}", Symbol, order.Data.ErrorMessage));

					return false;
				}

				orderId = order.Data.AlgoOrderId.Value;

				Logger.Write(string.Format("OrdersHandler.PlaceShortStopLossMarketOrder({0}): Price = {1}, Volume = {2}, Id = {3} [OK]", Symbol, price, volume, order.Data.AlgoOrderId.Value));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.PlaceShortStopLossMarketOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool CancelAlgoOrder(long orderId)
		{
			try
			{
				if(orderId == default)
				{
					Logger.Write(string.Format("OrdersHandler.CancelAlgoOrder({0}): Invalid OrderId", Symbol));

					return false;
				}

				var requests = new List<OkxAlgoOrderRequest>()
				{
					new OkxAlgoOrderRequest() { Instrument = Symbol, AlgoOrderId = orderId }
				};

				var result = Client.OrderBookTrading.AlgoTrading.CancelAlgoOrderAsync(requests).Result;

				if(result.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.CancelAlgoOrder({0}): {1}", Symbol, result.Error.Message));

					return false;
				}

				if(result?.Data?.ErrorCode != "0")
				{
					Logger.Write(string.Format("OrdersHandler.CancelAlgoOrder({0}): {1}", Symbol, result.Data.ErrorMessage));

					return false;
				}

				Logger.Write(string.Format("Order {0} Cancelled", orderId));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.CancelAlgoOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool CancelOrder(long orderId)
		{
			try
			{
				if(orderId == default)
				{
					Logger.Write(string.Format("OrdersHandler.CancelOrder({0}): Invalid OrderId", Symbol));

					return false;
				}

				var result = Client.OrderBookTrading.Trade.CancelOrderAsync(Symbol, orderId).Result;

				if(result.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.CancelOrder({0}): {1}", Symbol, result.Error.Message));

					return false;
				}

				if(result?.Data?.ErrorCode != default)
				{
					return CancelAlgoOrder(orderId);
				}

				Logger.Write(string.Format("Order {0} Cancelled", orderId));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.CancelOrder({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool CancelAllOrders()
		{
			try
			{
				if(UpdateOrdersList() == false)
				{
					return false;
				}

				if(Orders.Count == default)
				{
					return true;
				}

				var requests = new List<OkxOrderCancelRequest>();

				foreach(var record in Orders)
				{
					requests.Add(new OkxOrderCancelRequest() { InstrumentId = Symbol, OrderId = record.OrderId });
				}

				var result = Client.OrderBookTrading.Trade.CancelMultipleOrdersAsync(requests).Result;

				if(result.Success == false)
				{
					Logger.Write(string.Format("OrdersHandler.CancelAllOrders({0}): {1}", Symbol, result.Error.Message));

					return false;
				}

				foreach(var record in result.Data)
				{
					Logger.Write(string.Format("Order {0} Cancelled", record.OrderId.Value));
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrdersHandler.CancelAllOrders({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public List<Order> GetOrdersList()
		{
			List<Order> orders = default;

			lock(LockOrdersList)
			{
				orders = new List<Order>(Orders);
			}

			return orders;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
