using System;
using System.IO;

namespace RoverBot
{
	public static class Logger
	{
		public static bool IsConsoleEnabled { get; set; } = default;
		
		private const string LogFilePath = "RoverBot.txt";
		
		private static object LockFile = new object();

		private static void Print(string str)
		{
			if(IsConsoleEnabled)
			{
				try
				{
					Console.Write(str);
				}
				catch
				{
					
				}
			}
		}

		public static void Write(string str)
		{
			try
			{
				str = str.Replace('\n', ' ');
				str = str.Replace('\r', ' ');

				Print(str + "\n\n");
			}
			catch(Exception exception)
			{
				Print(exception + "\n\n");
			}
			
			try
			{
				string time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ");
				
				lock(LockFile)
				{
					File.AppendAllText(LogFilePath, time + str + "\r\n\r\n");
				}
			}
			catch(Exception exception)
			{
				Print(exception + "\n\n");
			}
		}
	}
}

