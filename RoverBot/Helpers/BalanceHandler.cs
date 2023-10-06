using System;
using System.Timers;
using System.Linq;

using OKX.Api;
using OKX.Api.Models.TradingAccount;

namespace RoverBot
{
	public sealed class BalanceHandler : IBalanceHandler
	{
		public const int TimerElapsedTime = 1000;

		public const int BalanceExpirationTime = 60000;

		public readonly string Currency;

		public decimal TotalBalance { get; private set; }

		public decimal AvailableBalance { get; private set; }

		public decimal FrozenBalance { get; private set; }

		public bool IsAvailable { get; private set; }

		private readonly OKXRestApiClient Client;

		private readonly OKXWebSocketApiClient Socket;

		private readonly Timer MainTimer;

		private DateTime LastUpdationTime;

		public BalanceHandler(OKXRestApiClient client, OKXWebSocketApiClient socket, string currency = "USDT")
		{
			if(client == default)
			{
				throw new Exception();
			}

			if(socket == default)
			{
				throw new Exception();
			}

			if(currency == default)
			{
				throw new Exception();
			}

			Client = client;

			Socket = socket;

			Currency = currency;

			IsAvailable = UpdateBalance();

			Socket.TradingAccount.SubscribeToAccountUpdatesAsync(SocketUpdation);

			MainTimer = new Timer(TimerElapsedTime);

			MainTimer.Elapsed += TimerElapsed;

			MainTimer.Start();
		}

		private void SocketUpdation(OkxAccountBalance data)
		{
			try
			{
				var balance = data?.Details?.FirstOrDefault(x => x.Currency == Currency);

				if(balance == default)
				{
					Logger.Write("BalanceHandler.SocketUpdation: Invalid Currency");

					return;
				}

				AvailableBalance = balance.AvailableBalance.Value;

				FrozenBalance = balance.FrozenBalance.Value;

				TotalBalance = AvailableBalance + FrozenBalance;

				LastUpdationTime = DateTime.Now;

				IsAvailable = true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("BalanceHandler.SocketUpdation({0}): {1}", Currency, exception.Message));
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				if(LastUpdationTime.AddMilliseconds(BalanceExpirationTime) < DateTime.Now)
				{
					Logger.Write(string.Format("BalanceHandler.SocketUpdation({0}): Expired", Currency));

					IsAvailable = UpdateBalance();
				}
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("BalanceHandler.TimerElapsed({0}): {1}", Currency, exception.Message));
			}
		}

		private bool UpdateBalance()
		{
			try
			{
				var balance = Client.TradingAccount?.GetAccountBalanceAsync().Result?.Data?.Details?.FirstOrDefault(x => x.Currency == Currency);
				
				if(balance == default)
				{
					Logger.Write("BalanceHandler.UpdateBalance: Invalid Currency");

					return false;
				}

				AvailableBalance = balance.AvailableBalance.Value;
				
				FrozenBalance = balance.FrozenBalance.Value;
				
				TotalBalance = AvailableBalance + FrozenBalance;
				
				LastUpdationTime = DateTime.Now;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("BalanceHandler.UpdateBalance({0}): {1}", Currency, exception.Message));

				return false;
			}
		}

		public decimal GetTotalBalance()
		{
			return TotalBalance;
		}

		public decimal GetAvailableBalance()
		{
			return AvailableBalance;
		}

		public decimal GetFrozenBalance()
		{
			return FrozenBalance;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
