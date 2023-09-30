using System;
using System.IO;
using System.Threading.Tasks;

namespace RoverBot
{
	public static class Logger
	{
		private const string LogFileName = "RoverBot.txt";
		
		public static readonly string LogFilePath;

		private static readonly object LockFile = new object();

		public static bool IsConsoleEnabled { get; set; } = true;

		static Logger()
		{
			try
			{
				string directory = AppDomain.CurrentDomain.BaseDirectory;

				LogFilePath = string.Concat(directory, LogFileName);
			}
			catch
			{
				LogFilePath = LogFileName;
			}
		}

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
				catch
				{

				}
			}

			Task.Run(() =>
			{
				try
				{
					lock(LockFile)
					{
						File.AppendAllText(LogFilePath, record);
					}
				}
				catch
				{
					
				}
			});
		}
	}
}
