using OKX.Api.Enums;
using System;

namespace RoverBot
{
	public enum PositionSide
	{
		Unknown,

		NoPosition,
		LongPosition,
		ShortPosition,
	}

	public static class PositionSideExtensions
	{
		public static string ToCustomString(this PositionSide positionSide)
		{
			switch(positionSide)
			{
				case PositionSide.NoPosition:
					return "NoPosition";
				
				case PositionSide.LongPosition: 
					return "LongPosition";
				
				case PositionSide.ShortPosition: 
					return "ShortPosition";
			}

			throw new Exception();
		}

		public static PositionSide GetPositionSide(OkxPositionSide positionSide)
		{
			try
			{
				switch(positionSide)
				{
					case OkxPositionSide.Net:
						return PositionSide.NoPosition;
					
					case OkxPositionSide.Long:
						return PositionSide.LongPosition;
					
					case OkxPositionSide.Short:
						return PositionSide.ShortPosition;
				}

				return PositionSide.Unknown;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionSideExtensions.GetPositionSide: {0}", exception.Message));

				return PositionSide.Unknown;
			}
		}
	}
}
