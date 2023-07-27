using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestChangeBot;

class Program
{
    static void Main(string[] args)
    {
        var botHandler = new TelegramBotHandler("6248006565:AAGd7yik-RwqW4yrO_X21pbYvvRpsR04QGM");
        botHandler.RunBotAsync().GetAwaiter().GetResult();
    }

}



