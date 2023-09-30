using System;
using System.IO;

namespace RoverBot
{
	public static class Logger
	{
		private const string LogFilePath = "RoverBot.txt";

		private static readonly object LockFile = new object();

		public static bool IsConsoleEnabled { get; set; } = true;

		public static void Write(string str)
		{
			string record = default;

			try
			{
				string date = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff\t");

				if(str == default)
				{
					str = "Invalid String";
				}

				str = str.Replace('\n', ' ');
				str = str.Replace('\r', ' ');

				record = string.Concat(date, str, "\r\n\r\n");
			}
			finally
			{
				if(record == default)
				{
					record = "Invalid Record\r\n\r\n";
				}
			}

			if(IsConsoleEnabled)
			{
				try
				{
					Console.Write(record);
				}
				finally
				{

				}
			}

			try
			{
				lock(LockFile)
				{
					File.AppendAllText(LogFilePath, record);
				}
			}
			finally
			{
				
			}
		}
	}
}
