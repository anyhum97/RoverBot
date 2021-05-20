using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Net;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RoverBot
{
	public static class TelegramBot
	{
		private const string Token = "1817772026:AAHjEtcxB_o048CVGVuJlVaFjgPtPtU9rHo";

		private static readonly TelegramBotClient Client = default;

		private static readonly List<long> ChatList = default;
		
		static TelegramBot()
		{
			try
			{
				Client = new TelegramBotClient(Token);

				Client.OnMessage += OnMessageReceived;

				ChatList = new List<long>();

				ChatList.Add(614503016);
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot: " + exception.Message);

				Client = null;
			}
		}

		public static bool IsValid()
		{
			try
			{
				if(Client == null)
				{
					return false;
				}

				return true;
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot.IsValid: " + exception.Message);

				return false;
			}
		}

		public static void Start()
		{
			try
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				if(IsValid())
				{
					Client.StartReceiving();
				}
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot.Start: " + exception.Message);
			}
		}

		public static void Send(string str)
		{
			if(IsValid())
			{
				try
				{
					foreach(var chat in ChatList)
					{
						Client.SendTextMessageAsync(new ChatId(chat), str);
					}
				}
				catch(Exception exception)
				{
					Logger.Write("TelegramBot.Send: " + exception.Message);
				}
			}
		}

		private static void Send(long chatId, string str)
		{
			if(IsValid())
			{
				try
				{
					Client.SendTextMessageAsync(new ChatId(chatId), str);
				}
				catch(Exception exception)
				{
					Logger.Write("TelegramBot.Send: " + exception.Message);
				}
			}
		}

		private static void OnMessageReceived(object sender, MessageEventArgs input)
		{
			try
			{
				if(input != null)
				{
					long chatId = input.Message.From.Id;

					if(ChatList.Contains(chatId))
					{
						if(input.Message.Type == MessageType.Text)
						{
							ProcessMessage(chatId, input.Message.Text);
						}
					}
				}
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot.OnMessageReceived: " + exception.Message);
			}
		}

		private static void ProcessMessage(long chatId, string str)
		{
			if(str == null)
			{
				return;
			}

			try
			{
				str = str.ToLower();

				if(str == "start")
				{
					TradeBot.IsTrading = true;
					
					Send("Торговля включена");
					
					return;
				}
				
				if(str == "stop")
				{
					TradeBot.IsTrading = false;
					
					Send("Торговля отключена");
					
					return;
				}
				
				if(str.Contains("balance") || str == "b")
				{
					StringBuilder stringBuilder = new StringBuilder();

					stringBuilder.Append("Balance: ");

					stringBuilder.Append(Format(TradeBot.Balance1, 4));
					stringBuilder.Append(" ");
					stringBuilder.Append(TradeBot.Currency1);
					stringBuilder.Append(", ");

					stringBuilder.Append(Format(TradeBot.Balance2, 6));
					stringBuilder.Append(" ");
					stringBuilder.Append(TradeBot.Currency2);

					if(TradeBot.Currency2 != "BNB")
					{
						stringBuilder.Append(", ");
						stringBuilder.Append(Format(TradeBot.FeeCoins, 4));
						stringBuilder.Append(" BNB");
					}

					stringBuilder.Append(", Total: ");
					stringBuilder.Append(Format(TradeBot.TotalBalance, 2));
					stringBuilder.Append(" USDT");
					
					Send(chatId, stringBuilder.ToString());
					
					return;
				}
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot.ProcessMessage: " + exception.Message);
			}
		}

		private static string Format(decimal value, int sign = 4)
		{
			try
			{
				sign = Math.Max(sign, 0);
				sign = Math.Min(sign, 8);

				return string.Format(CultureInfo.InvariantCulture, "{0:F" + sign + "}", value);
			}
			catch(Exception exception)
			{
				Logger.Write("TelegramBot.Format: " + exception.Message);

				return "Invalid Format";
			}
		}
	}
}

