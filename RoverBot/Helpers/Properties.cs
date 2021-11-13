namespace RoverBot
{
	public static partial class BinanceFutures
	{
		#region Balance

		private static object LockBalance = new object();

		private static decimal balance = default;

		public static decimal Balance
		{
			get
			{
				lock(LockBalance)
				{
					return balance;
				}
			}

			private set
			{
				lock(LockBalance)
				{
					balance = value;
				}
			}
		}

		#endregion

		#region LastBalance

		private static object LockLastBalance = new object();

		private static decimal lastBalance = default;

		public static decimal LastBalance
		{
			get
			{
				lock(LockLastBalance)
				{
					return lastBalance;
				}
			}

			private set
			{
				lock(LockLastBalance)
				{
					lastBalance = value;
				}
			}
		}

		#endregion

		#region TotalBalance

		private static object LockTotalBalance = new object();

		private static decimal totalBalance = default;

		public static decimal TotalBalance
		{
			get
			{
				lock(LockTotalBalance)
				{
					return totalBalance;
				}
			}

			private set
			{
				lock(LockTotalBalance)
				{
					totalBalance = value;
				}
			}
		}

		#endregion

		#region Frozen

		private static object LockFrozen = new object();

		private static decimal frozen = default;

		public static decimal Frozen
		{
			get
			{
				lock(LockFrozen)
				{
					return frozen;
				}
			}

			private set
			{
				lock(LockFrozen)
				{
					frozen = value;
				}
			}
		}

		#endregion

		#region FeePrice

		private static object LockFeePrice = new object();

		private static decimal feePrice = default;

		public static decimal FeePrice
		{
			get
			{
				lock(LockFeePrice)
				{
					return feePrice;
				}
			}

			private set
			{
				lock(LockFeePrice)
				{
					feePrice = value;
				}
			}
		}

		#endregion

		#region FeeBalance

		private static object LockFeeBalance = new object();

		private static decimal feeBalance = default;

		public static decimal FeeBalance
		{
			get
			{
				lock(LockFeeBalance)
				{
					return feeBalance;
				}
			}

			private set
			{
				lock(LockFeeBalance)
				{
					feeBalance = value;
				}
			}
		}

		#endregion

		#region FeeCoins

		private static object LockFeeCoins = new object();

		private static decimal feeCoins = default;

		public static decimal FeeCoins
		{
			get
			{
				lock(LockFeeCoins)
				{
					return feeCoins;
				}
			}

			private set
			{
				lock(LockFeeCoins)
				{
					feeCoins = value;
				}
			}
		}

		#endregion

		#region CurrentLeverage

		private static object LockCurrentLeverage = new object();

		private static int currentLeverage = default;

		public static int CurrentLeverage
		{
			get
			{
				lock(LockCurrentLeverage)
				{
					return currentLeverage;
				}
			}

			private set
			{
				lock(LockCurrentLeverage)
				{
					currentLeverage = value;
				}
			}
		}

		#endregion
		
		#region IsTrading

		private static object LockIsTrading = new object();

		private static bool isTrading = default;

		public static bool IsTrading
		{
			get
			{
				lock(LockIsTrading)
				{
					return isTrading;
				}
			}

			private set
			{
				lock(LockIsTrading)
				{
					isTrading = value;
				}
			}
		}

		#endregion

		#region InPosition

		private static object LockInPosition = new object();

		private static bool inPosition = default;

		public static bool InPosition
		{
			get
			{
				lock(LockInPosition)
				{
					return inPosition;
				}
			}

			private set
			{
				lock(LockInPosition)
				{
					inPosition = value;
				}
			}
		}

		#endregion
		
		#region State

		private static object LockState = new object();

		private static bool state = default;

		public static bool State
		{
			get
			{
				lock(LockState)
				{
					return state;
				}
			}

			private set
			{
				lock(LockState)
				{
					state = value;
				}
			}
		}

		#endregion
	}
}

