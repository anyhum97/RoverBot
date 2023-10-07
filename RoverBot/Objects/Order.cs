using System.Text;

namespace RoverBot
{
	public readonly struct Order
	{
		public readonly OrderType Type;

		public readonly decimal Price;

		public readonly decimal Volume;

		public readonly string OrderLabel;

		public readonly long? OrderId;

		public Order(OrderType type, decimal price, decimal volume, long? orderId = null, string orderLabel = null)
		{
			Type = type;
			Price = price;
			Volume = volume;
			OrderId = orderId;
			OrderLabel = orderLabel;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.Append(Type.ToCustomString());

			stringBuilder.Append(": Price = ");

			stringBuilder.Append(Price);

			stringBuilder.Append(", Volume = ");

			stringBuilder.Append(Volume);

			if(OrderLabel != default)
			{
				stringBuilder.Append(", Id = ");

				stringBuilder.Append(OrderLabel);
			}

			if(OrderId.HasValue)
			{
				stringBuilder.Append(", OrderId = ");

				stringBuilder.Append(OrderId.Value);
			}

			return stringBuilder.ToString();
		}
	}
}
