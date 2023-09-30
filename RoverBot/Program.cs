using System;
using System.Threading;

namespace RoverBot
{
	public static class Program
	{
		public const string CheckLine = "******************************************************************************";

		public const string Version = "2.05";
		
		public const string ApiKey = "0c3d85cc-bdf9-4e69-b8f2-ecf24493ccd6";

		public static void Main()
		{
			Logger.Write(CheckLine);

			Logger.Write(string.Format("RoverBot Version {0} Started", Version));

			Logger.Write(ApiKey);

			Exchange.GetBalance(out var balance);

			while(true)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
