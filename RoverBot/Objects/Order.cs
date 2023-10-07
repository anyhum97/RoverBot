namespace RoverBot
{
	public readonly struct Order
	{
		public readonly OrderType Type;

		public readonly decimal Price;

		public readonly decimal Volume;

		public Order(OrderType type, decimal price, decimal volume)
		{
			Type = type;
			Price = price;
			Volume = volume;
		}

		public override string ToString()
		{
			return string.Format("{0}, Price = {1}, Volume = {2}", Type.ToCustomString(), Price, Volume);
		}
	}
}
