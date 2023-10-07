using System;

namespace RoverBot
{
	public readonly struct Position
	{
		public readonly PositionSide PositionSide;

		public readonly decimal Volume;

		public Position(PositionSide positionSide, decimal volume)
		{
			PositionSide = positionSide;
			Volume = volume;
		}

		public override string ToString()
		{
			return string.Format("{0}: Volume = {1}", PositionSide.ToCustomString(), Volume);
		}
	}
}
