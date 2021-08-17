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
		private const string Token = "1970395292:AAHUasqefvgSNm0pH7BlFnIeBkGPyK0tCis";

		private static readonly TelegramBotClient Client = default;

		private static readonly List<long> ChatList = default;

		static TelegramBot()
		{
			try
			{
				Client = new TelegramBotClient(Token);

				Client.OnMessage += OnMessageReceived;

				ChatList = new List<long>();

				ChatList.Add(356884766);
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

				if(str.Contains("balance") || str == "b")
				{
					StringBuilder stringBuilder = new StringBuilder();
					
					stringBuilder.Append("Balance: ");
					stringBuilder.Append(Format(BinanceFutures.Balance, 2));
					stringBuilder.Append(" ");
					stringBuilder.Append(BinanceFutures.Currency1);
					
					stringBuilder.Append(", ");
					stringBuilder.Append(Format(BinanceFutures.FeeBalance, 2));
					stringBuilder.Append(" USDT for fees");
					
					stringBuilder.Append(", Frozen: ");
					stringBuilder.Append(Format(BinanceFutures.Frozen, 2));
					stringBuilder.Append(" " + BinanceFutures.Currency1);
					
					stringBuilder.Append(", Total: ");
					stringBuilder.Append(Format(BinanceFutures.TotalBalance+BinanceFutures.FeeBalance, 2));
					stringBuilder.Append(" " + BinanceFutures.Currency1);
					
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

