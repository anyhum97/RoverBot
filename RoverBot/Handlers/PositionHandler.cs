using System;
using System.Linq;
using System.Timers;

using OKX.Api;
using OKX.Api.Enums;

namespace RoverBot
{
	public sealed class PositionHandler : IPositionHandler
	{
		public const int TimerElapsedTime = 1000;

		public const int PositionExpirationTime = 10000;

		public Position PositionState { get; private set; }

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

			MainTimer = new Timer(TimerElapsedTime);

			MainTimer.Elapsed += TimerElapsed;

			MainTimer.Start();
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
				Logger.Write(string.Format("OrdersHandler.TimerElapsed({0}): {1}", Symbol, exception.Message));
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

				decimal volume = record.PositionsQuantity.Value;

				var positionSide = PositionSide.NoPosition;

				if(volume < 0.0m)
				{

				}

				PositionState = new Position(positionSide, volume);

				PositionState.ToString();

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

		public Position GetPosition()
		{
			return PositionState;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
