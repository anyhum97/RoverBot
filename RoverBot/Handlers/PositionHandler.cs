using System;
using System.Linq;
using System.Timers;

using OKX.Api;
using OKX.Api.Models.TradingAccount;
using OKX.Api.Enums;

namespace RoverBot
{
	public sealed class PositionHandler : IPositionHandler
	{
		public const int TimerElapsedTime = 1000;

		public const int PositionExpirationTime = 40000;

		public decimal PositionVolume { get; private set; }

		public bool IsAvailable { get; private set; }

		public readonly string Symbol;
		
		private readonly OKXRestApiClient Client;

		private readonly OKXWebSocketApiClient Socket;

		private DateTime LastUpdationTime;

		private readonly OkxInstrumentType InstrumentType;

		private readonly Timer MainTimer;

		public PositionHandler(OKXRestApiClient client, OKXWebSocketApiClient socket, string symbol, OkxInstrumentType instrumentType = OkxInstrumentType.Swap)
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

			IsAvailable = UpdatePosition();

			Socket.TradingAccount.SubscribeToPositionUpdatesAsync(SocketUpdation, InstrumentType, instrumentId: Symbol);

			MainTimer = new Timer(TimerElapsedTime);

			MainTimer.Elapsed += TimerElapsed;

			MainTimer.Start();
		}
		
		private void SocketUpdation(OkxPosition data)
		{
			try
			{
				PositionVolume = data.PositionsQuantity.Value;

				LastUpdationTime = DateTime.Now;

				IsAvailable = true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionHandler.SocketUpdation({0}): {1}", Symbol, exception.Message));
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				if(LastUpdationTime.AddMilliseconds(PositionExpirationTime) < DateTime.Now)
				{
					IsAvailable = UpdatePosition();
				}
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionHandler.TimerElapsed({0}): {1}", Symbol, exception.Message));
			}
		}

		public bool UpdatePosition()
		{
			try
			{
				var position = Client.TradingAccount.GetAccountPositionsAsync(InstrumentType, Symbol).Result;

				if(position.Success == false)
				{
					Logger.Write(string.Format("PositionHandler.UpdatePosition({0}): {1}", Symbol, position.Error.Message));

					return false;
				}

				var record = position?.Data?.FirstOrDefault(x => x.Instrument == Symbol);

				if(record == default)
				{
					Logger.Write(string.Format("PositionHandler.UpdatePosition({0}): Invalid Request", Symbol));

					return false;
				}

				PositionVolume = record.PositionsQuantity.Value;

				LastUpdationTime = DateTime.Now;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionHandler.UpdatePosition({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool SetLeverage(int leverage)
		{
			try
			{
				var result = Client.TradingAccount.SetAccountLeverageAsync(leverage, null, "BTC-USDT-SWAP", OkxMarginMode.Isolated, OkxPositionSide.Net).Result;

				if(result.Success == false)
				{
					Logger.Write(string.Format("PositionHandler.SetLeverage({0}): {1}", Symbol, result.Error.Message));

					return false;
				}

				var record = result?.Data?.FirstOrDefault(x => x.Instrument == Symbol);

				if(record == default)
				{
					Logger.Write(string.Format("PositionHandler.SetLeverage({0}): Invalid Request", Symbol));

					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionHandler.SetLeverage({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public bool SetIsolatedMargin()
		{
			return true;
		}

		public bool ClosePostion()
		{
			try
			{
				var result = Client.OrderBookTrading.Trade.ClosePositionAsync(Symbol, OkxMarginMode.Isolated).Result;

				if(result.Success == false)
				{
					Logger.Write(string.Format("PositionHandler.ClosePostion({0}): {1}", Symbol, result.Error.Message));

					return false;
				}
				
				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("PositionHandler.ClosePostion({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		public decimal GetPosition()
		{
			return PositionVolume;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
