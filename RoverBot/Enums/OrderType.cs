using System;

using OKX.Api.Models.Trade;
using OKX.Api.Enums;
using OKX.Api.Models.AlgoTrading;

namespace RoverBot
{
	public enum OrderType
	{
		Unknown,

		LongLimitOrder,
		ShortLimitOrder,

		LongTakeProfitMarketOrder,
		LongStopLossMarketOrder,
		ShortTakeProfitMarketOrder,
		ShortStopLossMarketOrder,
	}

	public static class OrderTypeExtensions
	{
		public static string ToCustomString(this OrderType type)
		{
			switch(type)
			{
				case OrderType.LongLimitOrder: 
					return "LongLimitOrder";
				
				case OrderType.ShortLimitOrder: 
					return "ShortLimitOrder";
				
				case OrderType.LongTakeProfitMarketOrder: 
					return "LongTakeProfitMarketOrder";
				
				case OrderType.LongStopLossMarketOrder: 
					return "LongStopLossMarketOrder";
				
				case OrderType.ShortTakeProfitMarketOrder: 
					return "ShortTakeProfitMarketOrder";
				
				case OrderType.ShortStopLossMarketOrder: 
					return "ShortStopLossMarketOrder";
			}

			throw new Exception();
		}

		public static OrderType GetOrderType(OkxOrder order)
		{
			try
			{
				if(order.OrderType == OkxOrderType.LimitOrder)
				{
					if(order.OrderSide == OkxOrderSide.Buy)
					{
						return OrderType.LongLimitOrder;
					}

					if(order.OrderSide == OkxOrderSide.Sell)
					{
						return OrderType.ShortLimitOrder;
					}
				}

				return OrderType.Unknown;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrderTypeExtensions.GetOrderType: {0}", exception.Message));

				return OrderType.Unknown;
			}
		}

		public static OrderType GetOrderType(OkxAlgoOrder order)
		{
			try
			{
				if(order.OrderType == OkxAlgoOrderType.Conditional)
				{
					if(order.OrderSide == OkxOrderSide.Sell)
					{
						if(order.TakeProfitTriggerPrice != default)
						{
							return OrderType.LongTakeProfitMarketOrder;
						}

						if(order.StopLossOrderPrice != default)
						{
							return OrderType.LongStopLossMarketOrder;
						}
					}

					if(order.OrderSide == OkxOrderSide.Buy)
					{
						if(order.TakeProfitTriggerPrice != default)
						{
							return OrderType.ShortTakeProfitMarketOrder;
						}

						if(order.StopLossOrderPrice != default)
						{
							return OrderType.ShortStopLossMarketOrder;
						}
					}
				}

				return OrderType.Unknown;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("OrderTypeExtensions.GetOrderType: {0}", exception.Message));

				return OrderType.Unknown;
			}
		}
	}
}
