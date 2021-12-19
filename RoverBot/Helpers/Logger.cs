using System;
using System.IO;

namespace RoverBot
{
	public static class Logger
	{
		private const string LogFilePath = "RoverBot.txt";
		
		private static object LockFile = new object();

		public static void Write(string str)
		{
			try
			{
				if(str == default)
				{
					str = "Invalid String";
				}

				str = str.Replace('\n', ' ');
				str = str.Replace('\r', ' ');
			}
			catch
			{
				
			}
			
			try
			{
				lock(LockFile)
				{
					File.AppendAllText(LogFilePath, string.Format("{0:dd.MM.yyyy HH:mm:ss} {1}\r\n\r\n", DateTime.Now, str));
				}
			}
			catch
			{
				
			}
		}
	}
}

