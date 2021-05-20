using System;
using System.Threading;
using System.IO;

namespace RoverBot
{
	public static class Logger
	{
		public static bool IsConsoleEnabled { get; set; } = true;
		
		private const string LogFilePath = "RoverBot.txt";
		
		private const long MaxFileLength = 35267366;
		
		private static void Print(string str)
		{
			try
			{
				if(IsConsoleEnabled)
				{
					Console.Write(str);
				}
			}
			catch
			{
				
			}
		}

		public static void Write(string str)
		{
			try
			{
				str = str.Replace('\n', ' ');

				Print(str + "\n\n");

				if(File.Exists(LogFilePath))
				{
					FileInfo fileInfo = new FileInfo(LogFilePath);
					
					if(fileInfo.Length > MaxFileLength)
					{
						File.Delete(LogFilePath);

						Thread.Sleep(20);
					}
				}
			}
			catch(Exception exception)
			{
				Print(exception + "\n\n");
			}
			
			try
			{
				string currentMoment = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ");
				
				File.AppendAllText(LogFilePath, currentMoment + str + "\r\n\r\n");
			}
			catch(Exception exception)
			{
				Print(exception + "\n\n");
			}
		}
	}
}

