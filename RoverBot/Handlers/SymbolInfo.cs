using System;
using System.Timers;
using System.Globalization;
using System.Linq;

using OKX.Api.Enums;
using OKX.Api;

namespace RoverBot
{
	public sealed class SymbolInfo : ISymbolInfo
	{
		public const int TimerElapsedTime = 10000;

		public const int TimerElapsedLongPeriod = 600000;

		public decimal MinLot { get; private set; }

		public decimal PriceTick { get; private set; }

		public int PricePrecision { get; private set; }

		public int MinLeverage { get; private set; }

		public int MaxLeverage { get; private set; }

		public bool IsAvailable { get; private set; }

		public readonly string Symbol;

		private readonly OKXRestApiClient Client;

		private readonly OkxInstrumentType InstrumentType;

		private readonly Timer MainTimer;

		public SymbolInfo(OKXRestApiClient client, string symbol, OkxInstrumentType instrumentType = OkxInstrumentType.Swap, int pricePrecision = default)
		{
			if(client == default)
			{
				throw new Exception();
			}

			if(symbol == default)
			{
				throw new Exception();
			}

			Client = client;

			Symbol = symbol;

			InstrumentType = instrumentType;

			PricePrecision = pricePrecision;

			IsAvailable = UpdateSymbolInfo();

			MainTimer = new Timer(TimerElapsedTime);

			MainTimer.Elapsed += TimerElapsed;

			MainTimer.Start();
		}

		public bool UpdateSymbolInfo()
		{
			try
			{
				var instruments = Client.PublicData.GetInstrumentsAsync(InstrumentType, instrumentId: Symbol).Result;

				var record = instruments?.Data?.FirstOrDefault(x => x.Instrument == Symbol);

				if(record == default)
				{
					Logger.Write(string.Format("SymbolInfo.UpdateSymbolInfo({0}): Invalid Request", Symbol));

					return false;
				}

				MinLot = record.LotSize;

				PriceTick = record.TickSize;

				if(PricePrecision == default)
				{
					var parts = PriceTick.ToString(CultureInfo.InvariantCulture).Split('.');

					if(parts.Length != 2)
					{
						Logger.Write(string.Format("SymbolInfo.UpdateSymbolInfo({0}): Invalid Symbol Format", Symbol));

						return false;
					}

					PricePrecision = parts[1].Count();
				}

				MinLeverage = 1;

				MaxLeverage = (int)record.MaximumLeverage.Value;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("SymbolInfo.UpdateSymbolInfo({0}): {1}", Symbol, exception.Message));

				return false;
			}
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				if(IsAvailable)
				{
					if(MainTimer.Interval == TimerElapsedLongPeriod)
					{
						UpdateSymbolInfo();
					}
					else
					{
						MainTimer.Stop();

						MainTimer.Interval = TimerElapsedLongPeriod;

						MainTimer.Start();
					}
				}
				else
				{
					IsAvailable = UpdateSymbolInfo();
				}
			}
			catch(Exception exception)
			{
				Logger.Write(string.Format("SymbolInfo.TimerElapsed({0}): {1}", Symbol, exception.Message));
			}
		}

		public decimal GetMinLot()
		{
			return MinLot;
		}

		public decimal GetPriceTick()
		{
			return PriceTick;
		}

		public int GetPricePrecision()
		{
			return PricePrecision;
		}

		public int GetMinLeverage()
		{
			return MinLeverage;
		}

		public int GetMaxLeverage()
		{
			return MaxLeverage;
		}

		public bool GetHandlerState()
		{
			return IsAvailable;
		}
	}
}
