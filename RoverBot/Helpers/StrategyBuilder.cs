using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

using SharpLearning.RandomForest.Learners;
using SharpLearning.RandomForest.Models;

using Binance.Net;
using Binance.Net.Enums;

namespace RoverBot
{
	public static class StrategyBuilder
	{
		public const string ModelPath = "TradeModel.xml";

		public const double Learn = 0.8;

		public const double Test = 0.2;

		public const decimal Percent = 0.02m;

		public const double Threshold = 0.66;

		public const double Border = 0.95;

		public const int StartOffset = 1024;

		public const int StopOffset = 1440;

		public const int BufferSize = 45664;

		public const int Expiration = 600;

		public const int Seed = 108377437;

		public const int Trees = 100;

		public static bool UpdateStrategy(string symbol, out ClassificationForestModel model)
		{
			model = default;
			
			try
			{
				if(LoadTradeModel(out model))
				{
					return true;
				}

				Logger.Write("UpdateStrategy: Loading History...");

				if(LoadHistory(symbol, BufferSize, out var history))
				{
					Logger.Write("UpdateStrategy: Building Model...");

					if(BuildStrategy(history, out model))
					{
						Logger.Write("UpdateStrategy: Ready");

						return true;
					}
					else
					{
						Logger.Write("UpdateStrategy: Sleep");

						Thread.Sleep(600000);
					}
				}
				
				return false;
			}
			catch(Exception exception)
			{
				Logger.Write("UpdateStrategy: " + exception.Message);

				return false;
			}
		}

		private static bool LoadTradeModel(out ClassificationForestModel model)
		{
			model = default;

			try
			{
				if(File.Exists(ModelPath) == false)
				{
					return false;
				}

				FileInfo fileInfo = new FileInfo(ModelPath);

				if(fileInfo.LastWriteTime.AddDays(1.0) < DateTime.Now)
				{
					return false;
				}

				model = ClassificationForestModel.Load(() => new StreamReader(ModelPath));

				if(model == null)
				{
					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("LoadTradeModel: " + exception.Message);

				return false;
			}
		}

		private static bool LoadHistory(string symbol, int count, out List<Candle> history)
		{
			history = new List<Candle>();

			try
			{
				BinanceClient client = new BinanceClient();

				DateTime startTime = DateTime.Now.AddMinutes(-count-1).ToUniversalTime();

				const int pageSize = 1000;

				const int attempts = 3;

				int pages = count / pageSize;

				int remainder = count - pages*pageSize;

				for(int i=0; i<pages; ++i)
				{
					bool flag = false;

					for(int j=0; j<attempts; ++j)
					{
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:pageSize);
						
						if(responce.Success)
						{
							startTime = startTime.AddMinutes(pageSize);
							
							foreach(var record in responce.Data)
							{
								history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
							}

							flag = true;

							break;
						}
						else
						{
							Thread.Sleep(1000);
						}
					}

					if(flag == false)
					{
						return false;
					}
				}

				if(remainder > 0)
				{
					bool flag = false;

					for(int j=0; j<attempts; ++j)
					{
						var responce = client.Spot.Market.GetKlines(symbol, KlineInterval.OneMinute, startTime, limit:remainder);
						
						if(responce.Success)
						{
							foreach(var record in responce.Data)
							{
								history.Add(new Candle(record.CloseTime.ToLocalTime(), record.Open, record.Close, record.Low, record.High));
							}

							flag = true;

							break;
						}
						else
						{
							Thread.Sleep(1000);
						}
					}

					if(flag == false)
					{
						return false;
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("LoadHistory: " + exception.Message);

				return false;
			}
		}

		private static bool BuildStrategy(List<Candle> history, out ClassificationForestModel model)
		{
			model = default;

			try
			{
				Random RandomDevice = new Random(Seed);

				List<double[]> inputs = new List<double[]>();

				List<double> results = new List<double>();

				for(int i=StartOffset; i<history.Count-StopOffset; ++i)
				{
					double random = RandomDevice.NextDouble();

					if(random <= Learn)
					{
						bool state = true;

						decimal delta1 = default;
						decimal delta2 = default;
						decimal delta3 = default;
						decimal delta4 = default;
						decimal delta5 = default;

						decimal trand1 = default;
						decimal trand2 = default;
						decimal trand3 = default;
						decimal trand4 = default;
						decimal trand5 = default;

						decimal factor1 = default;
						decimal factor2 = default;
						decimal factor3 = default;
						decimal factor4 = default;
						decimal factor5 = default;

						decimal quota1 = default;
						decimal quota2 = default;
						decimal quota3 = default;
						decimal quota4 = default;
						decimal quota5 = default;
						decimal quota6 = default;

						int isEntry = default;

						state = state && GetDelta(history, i, 16, out delta1);
						state = state && GetDelta(history, i, 24, out delta2);
						state = state && GetDelta(history, i, 36, out delta3);
						state = state && GetDelta(history, i, 48, out delta4);
						state = state && GetDelta(history, i, 64, out delta5);

						state = state && GetTrand(history, i, 64, out trand1);
						state = state && GetTrand(history, i, 128, out trand2);
						state = state && GetTrand(history, i, 256, out trand3);
						state = state && GetTrand(history, i, 512, out trand4);
						state = state && GetTrand(history, i, 1024, out trand5);

						state = state && GetDeviationFactor(history, i, 16, out factor1);
						state = state && GetDeviationFactor(history, i, 24, out factor2);
						state = state && GetDeviationFactor(history, i, 32, out factor3);
						state = state && GetDeviationFactor(history, i, 64, out factor4);
						state = state && GetDeviationFactor(history, i, 128, out factor5);
						
						state = state && GetQuota(history, i, 24, out quota1);
						state = state && GetQuota(history, i, 64, out quota2);
						state = state && GetQuota(history, i, 128, out quota3);
						state = state && GetQuota(history, i, 256, out quota4);
						state = state && GetQuota(history, i, 512, out quota5);
						state = state && GetQuota(history, i, 1024, out quota6);
						
						state = state && IsEntry(history, i, Expiration, Percent, out isEntry);

						if(state == false)
						{
							return false;
						}

						double[] buffer = new double[]
						{
							(double)delta1,
							(double)delta2,
							(double)delta3,
							(double)delta4,
							(double)delta5,
							
							(double)trand1,
							(double)trand2,
							(double)trand3,
							(double)trand4,
							(double)trand5,
							
							(double)factor1,
							(double)factor2,
							(double)factor3,
							(double)factor4,
							(double)factor5,
							
							(double)quota1,
							(double)quota2,
							(double)quota3,
							(double)quota4,
							(double)quota5,
							(double)quota6,
						};

						inputs.Add(buffer);

						results.Add(isEntry);
					}
				}

				var learner = new ClassificationRandomForestLearner(trees: Trees, seed: Seed);

				model = learner.Learn(inputs.ToArray(), results.ToArray());

				RandomDevice = new Random(Seed);

				inputs = new List<double[]>();

				results = new List<double>();

				for(int i=StartOffset; i<history.Count-StopOffset; ++i)
				{
					double random = RandomDevice.NextDouble();

					if(random >= Learn && random <= Learn + Test)
					{
						bool state = true;

						decimal delta1 = default;
						decimal delta2 = default;
						decimal delta3 = default;
						decimal delta4 = default;
						decimal delta5 = default;

						decimal trand1 = default;
						decimal trand2 = default;
						decimal trand3 = default;
						decimal trand4 = default;
						decimal trand5 = default;

						decimal factor1 = default;
						decimal factor2 = default;
						decimal factor3 = default;
						decimal factor4 = default;
						decimal factor5 = default;

						decimal quota1 = default;
						decimal quota2 = default;
						decimal quota3 = default;
						decimal quota4 = default;
						decimal quota5 = default;
						decimal quota6 = default;

						int isEntry = default;

						state = state && GetDelta(history, i, 16, out delta1);
						state = state && GetDelta(history, i, 24, out delta2);
						state = state && GetDelta(history, i, 36, out delta3);
						state = state && GetDelta(history, i, 48, out delta4);
						state = state && GetDelta(history, i, 64, out delta5);

						state = state && GetTrand(history, i, 64, out trand1);
						state = state && GetTrand(history, i, 128, out trand2);
						state = state && GetTrand(history, i, 256, out trand3);
						state = state && GetTrand(history, i, 512, out trand4);
						state = state && GetTrand(history, i, 1024, out trand5);

						state = state && GetDeviationFactor(history, i, 16, out factor1);
						state = state && GetDeviationFactor(history, i, 24, out factor2);
						state = state && GetDeviationFactor(history, i, 32, out factor3);
						state = state && GetDeviationFactor(history, i, 64, out factor4);
						state = state && GetDeviationFactor(history, i, 128, out factor5);
						
						state = state && GetQuota(history, i, 24, out quota1);
						state = state && GetQuota(history, i, 64, out quota2);
						state = state && GetQuota(history, i, 128, out quota3);
						state = state && GetQuota(history, i, 256, out quota4);
						state = state && GetQuota(history, i, 512, out quota5);
						state = state && GetQuota(history, i, 1024, out quota6);
						
						state = state && IsEntry(history, i, Expiration, Percent, out isEntry);

						if(state == false)
						{
							return false;
						}

						double[] buffer = new double[]
						{
							(double)delta1,
							(double)delta2,
							(double)delta3,
							(double)delta4,
							(double)delta5,
							
							(double)trand1,
							(double)trand2,
							(double)trand3,
							(double)trand4,
							(double)trand5,
							
							(double)factor1,
							(double)factor2,
							(double)factor3,
							(double)factor4,
							(double)factor5,
							
							(double)quota1,
							(double)quota2,
							(double)quota3,
							(double)quota4,
							(double)quota5,
							(double)quota6,
						};

						inputs.Add(buffer);

						results.Add(isEntry);
					}
				}

				var predictions = model.PredictProbability(inputs.ToArray());

				int good = default;

				int fail = default;

				for(int i=0; i<predictions.Length; ++i)
				{
					if(predictions[i].Probabilities[1] > Threshold)
					{
						if(results[i] == 1.0)
						{
							++good;
						}
						else
						{
							++fail;
						}
					}
				}

				Logger.Write(string.Format("BuildStrategy: {0} | {1}", good, fail));

				if(good + fail == default)
				{
					return false;
				}

				double factor = (double)good / (good + fail);

				if(factor < Border)
				{
					Logger.Write("BuildStrategy: Invalid Percent");

					return false;
				}

				model.Save(() => new StreamWriter(ModelPath));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("BuildStrategy: " + exception.Message);

				return false;
			}
		}

		private static bool GetDelta(List<Candle> history, int index, int window, out decimal delta)
		{
			delta = default;

			try
			{
				for(int i=index-window; i<index; ++i)
				{
					decimal price1 = history[i].Close;

					decimal price2 = history[i+1].Close;

					delta += price2 - price1;
				}

				delta /= history[index].Close;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetDelta: " + exception.Message);

				return false;
			}
		}

		private static bool GetTrand(List<Candle> history, int index, int window, out decimal trand)
		{
			trand = default;

			try
			{
				decimal price2 = history[index].Close;

				for(int i=index-window; i<index; ++i)
				{
					decimal price1 = history[i].Close;

					decimal delta = price2 - price1;

					if(Math.Abs(delta) > Math.Abs(trand))
					{
						trand = delta;
					}
				}

				trand = trand / price2;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetTrand: " + exception.Message);

				return false;
			}
		}

		private static bool GetAverage(List<Candle> history, int index, int window, out decimal average)
		{
			average = default;

			try
			{
				for(int i=index-window+1; i<index+1; ++i)
				{
					decimal price = history[i].Close;

					average += price;
				}

				average /= window;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetAverage: " + exception.Message);

				return false;
			}
		}

		private static bool GetDeviation(List<Candle> history, int index, int window, out decimal average, out decimal deviation)
		{
			average = default;

			deviation = default;

			try
			{
				if(GetAverage(history, index, window, out average) == false)
				{
					return false;
				}
				
				deviation = default;

				for(int i=index-window+1; i<index+1; ++i)
				{
					decimal price = history[i].Close;

					deviation += (price - average) * (price - average);
				}

				deviation = (decimal)Math.Sqrt((double)deviation / (window - 1));

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetDeviation: " + exception.Message);

				return false;
			}
		}

		private static bool GetDeviationFactor(List<Candle> history, int index, int window, out decimal factor)
		{
			factor = default;

			try
			{
				if(GetDeviation(history, index, window, out decimal average, out decimal deviation) == false)
				{
					return false;
				}

				decimal delta = average - history[index].Close;

				factor = delta / deviation;

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetDeviationFactor: " + exception.Message);

				return false;
			}
		}

		private static bool GetQuota(List<Candle> history, int index, int window, out decimal quota)
		{
			quota = default;

			try
			{
				decimal more = default;

				decimal less = default;

				decimal price2 = history[index].Close;

				for(int i=index-window+1; i<index; ++i)
				{
					decimal price1 = history[i].Close;

					decimal delta = Math.Abs(price1 - price2);

					if(price1 > price2)
					{
						more += delta;
					}
					else
					{
						less += delta;
					}
				}

				if(more + less != default)
				{
					quota = more / (more + less);
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("GetQuota: " + exception.Message);

				return false;
			}
		}

		private static bool IsEntry(List<Candle> history, int index, int window, decimal percent, out int value)
		{
			value = default;

			try
			{
				decimal price1 = history[index].Close;

				for(int i=index+1; i<index+1+window; ++i)
				{
					decimal price2 = history[i].High;

					decimal factor = (price2 - price1)/price1;

					if(factor >= percent)
					{
						value = 1;

						return true;
					}
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("IsEntry: " + exception.Message);

				return false;
			}
		}
	}
}

