using System;
using System.Globalization;
using System.Text;

namespace RoverBot
{
	public class SellOrder
	{
		public readonly long OrderId;

		public readonly decimal Volume;

		public readonly decimal Price;

		public SellOrder(long orderId, decimal volume, decimal price)
		{
			try
			{
				OrderId = orderId;

				Volume = volume;

				Price = price;
			}
			catch(Exception exception)
			{
				Logger.Write("SellOrder: " + exception.Message);
			}
		}

		public string Format()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();

				stringBuilder.Append(Format(Volume, TradeBot.VolumePrecision));

				stringBuilder.Append(" монеты ");

				stringBuilder.Append(TradeBot.Currency2);

				stringBuilder.Append(" ожидают продажи по цене ");

				stringBuilder.Append(Format(Price, TradeBot.PricePrecision));

				return stringBuilder.ToString();
			}
			catch(Exception exception)
			{
				Logger.Write("SellOrder.Format: " + exception.Message);

				return "Invalid Format";
			}
		}

		public string Filled()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();

				stringBuilder.Append(Format(Volume, TradeBot.VolumePrecision));

				stringBuilder.Append(" монеты ");

				stringBuilder.Append(TradeBot.Currency2);

				stringBuilder.Append(" были проданы по цене ");

				stringBuilder.Append(Format(Price, TradeBot.PricePrecision));

				return stringBuilder.ToString();
			}
			catch(Exception exception)
			{
				Logger.Write("SellOrder.Filled: " + exception.Message);

				return "Invalid Format";
			}
		}

		private static string Format(decimal value, int sign = 4)
		{
			try
			{
				sign = Math.Max(sign, 0);
				sign = Math.Min(sign, 8);

				return string.Format(CultureInfo.InvariantCulture, "{0:F" + sign + "}", value);
			}
			catch(Exception exception)
			{
				Logger.Write("SellOrder.Format: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

