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

				ChatList.Add(768427512);
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
					if(TradeBot.IsTrading == false)
					{
						TradeBot.IsTrading = true;
						
						Send("Торговля включена");
					}
					else
					{
						Send(chatId, "Торговля включена");
					}

					return;
				}
				
				if(str == "stop")
				{
					if(TradeBot.IsTrading == true)
					{
						TradeBot.IsTrading = false;
						
						Send("Торговля отключена");
					}
					else
					{
						Send(chatId, "Торговля отключена");
					}
					
					return;
				}
				
				if(str.Contains("order") || str == "o")
				{
					int count = TradeBot.SellOrders.Count;

					if(count == 0)
					{
						Send(chatId, "Нет открытых ордеров");

						return;
					}
					
					StringBuilder stringBuilder = new StringBuilder();
					
					const int window = 16;

					for(int i=0; i<count && i<window; ++i)
					{
						stringBuilder.Append(TradeBot.SellOrders[i].Format());

						stringBuilder.Append("\n\n");
					}

					if(count > window)
					{
						if(count-window <= 4)
						{
							switch(count-window)
							{
								case 1: stringBuilder.Append("И ещё 1 ордер..."); break;
								case 2: stringBuilder.Append("И ещё 2 ордера..."); break;
								case 3: stringBuilder.Append("И ещё 3 ордера..."); break;
								case 4: stringBuilder.Append("И ещё 4 ордера..."); break;
							}
						}
						else
						{
							stringBuilder.Append("И ещё ");
							stringBuilder.Append(count-window);
							stringBuilder.Append("ордеров...");
						}

						stringBuilder.Append("\n\n");
					}

					stringBuilder.Append("Текущая цена: " + Format(WebSocketSpot.CurrentPrice, TradeBot.PricePrecision));

					Send(chatId, stringBuilder.ToString());

					return;
				}

				if(str.Contains("balance") || str == "b")
				{
					StringBuilder stringBuilder = new StringBuilder();

					stringBuilder.Append("Balance: ");

					stringBuilder.Append(Format(TradeBot.Balance1, TradeBot.CurrencyPrecision1));
					stringBuilder.Append(" ");
					stringBuilder.Append(TradeBot.Currency1);
					stringBuilder.Append(", ");

					stringBuilder.Append(Format(TradeBot.Balance2, TradeBot.CurrencyPrecision2));
					stringBuilder.Append(" ");
					stringBuilder.Append(TradeBot.Currency2);

					stringBuilder.Append(", ");
					stringBuilder.Append(Format(TradeBot.FeeCoins, TradeBot.CurrencyPrecision3));
					stringBuilder.Append(" BNB");

					stringBuilder.Append(", Total: ");
					stringBuilder.Append(Format(TradeBot.TotalBalance, TradeBot.CurrencyPrecision1));
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

