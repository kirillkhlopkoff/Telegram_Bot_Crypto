using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace TestChangeBot
{
    public class TelegramBotHandler
    {
        public static TelegramBotClient _client;
        public static CurrentCourse _currentCourse;

        private static Dictionary<long, string> _userSelectedBaseCurrencies = new Dictionary<long, string>();
        private static Dictionary<long, string> _userSelectedTargetCurrencies = new Dictionary<long, string>();

        public TelegramBotHandler(string apiKey)
        {
            _client = new TelegramBotClient(apiKey);
            _currentCourse = new CurrentCourse();
        }

        private static Dictionary<long, OperationState> _userOperations = new Dictionary<long, OperationState>();

        static string selectedTargetCurrency = string.Empty;

        public async Task RunBotAsync()
        {
            using var cts = new System.Threading.CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };
            _client.StartReceiving(UpdateAsync, Error, receiverOptions, cancellationToken: cts.Token);

            var me = await _client.GetMeAsync();
            Console.WriteLine($"Start speaking with @{me.Username}");
            Console.ReadLine();
            cts.Cancel();
        }

        private async static Task UpdateAsync(ITelegramBotClient client, Update update, System.Threading.CancellationToken token)
        {
            var message = update.Message;
            /*if (message == null)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.");
                return;
            }*/
            if (message?.Type == MessageType.Photo)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Спасибо, платеж отправлен в обработку. Ожидайте зачисления средств.");

                // Получаем ID фото, которое хотим переслать
                var photoId = message.Photo[0].FileId;

                // ID чата, куда нужно переслать фото (в данном случае ID технического чата)
                long technicalChatId = 900281273; // Замените на реальный ID вашего технического чата

                // Пересылаем фото в технический чат
                await client.ForwardMessageAsync(technicalChatId, message.Chat.Id, message.MessageId);

                return;
            }
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await Console.Out.WriteLineAsync($"{update.Message.Chat.FirstName} | {update.Message.Text}");

                await HandleMessage(client, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data != null)
            {
                await HandleCallbackQuery(client, update.CallbackQuery);
            }
        }

        static async Task HandleMessage(ITelegramBotClient client, Message message)
        {
            if (message == null)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.");
                return;
            }
            if (message.Text == "/start")
            {
                ReplyKeyboardMarkup keyboard = new(new[]
                {
                    new KeyboardButton[] { new KeyboardButton("💶 Обменять"), new KeyboardButton("👤 Личный кабинет") },
                    new KeyboardButton[] { new KeyboardButton("💬 Сообщество"), new KeyboardButton("📞 Поддержка") },
                    new KeyboardButton[] { new KeyboardButton("⚖️ Текущий курс") }
                })
                {
                    ResizeKeyboard = true
                };

                await client.SendTextMessageAsync(message.Chat.Id, "Выберите действие:", replyMarkup: keyboard);
            }
            else if (message.Text == "⚖️ Текущий курс")
            {
                await _currentCourse.SendCryptoCurrencyRates(message.Chat.Id);
            }
            else if (message.Text == "📞 Поддержка")
            {
                string buttonText = message.Text == "📞 Поддержка" ? "Связаться с поддержкой" : "Перейти в группу";
                string buttonUrl = "https://t.me/GrekKH"; // Здесь URL, на который должна вести ссылка

                var supportButton = new InlineKeyboardButton(buttonText)
                {
                    Url = buttonUrl
                };

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { supportButton } });

                await client.SendTextMessageAsync(message.Chat.Id, "Для связи с поддержкой нажмите на кнопку ниже:", replyMarkup: inlineKeyboard);
            }
            else if (message.Text == "💬 Сообщество")
            {
                // Создаем и отправляем сообщение с ссылкой-кнопкой
                var supportButton = new InlineKeyboardButton(string.Empty)
                {
                    Text = "Перейти в группу", // Указываем текст для кнопки
                    Url = "https://t.me/GrekKH" // Здесь URL, на который должна вести ссылка
                };

                var inlineKeyboard = new InlineKeyboardMarkup(new[] { new[] { supportButton } });

                await client.SendTextMessageAsync(message.Chat.Id, "Для связи с поддержкой нажмите на кнопку ниже:", replyMarkup: inlineKeyboard);
            }
            else if (message.Text == "💶 Обменять")
            {
                var cryptoCurrencies = new[]
                {
        new[] { "USDT", "TRX", "LTC" }, // Здесь можно добавить другие криптовалюты
        new[] { "BCH", "DAI", "BUSD" },
        new[] { "TON", "BTC", "DASH" },
        new[] { "XMR", "VERSE", "DOGE" },
        new[] { "USDC", "MATIC", "BNB" },
        new[] { "ETH" }
    };

                var inlineKeyboard = new InlineKeyboardMarkup(cryptoCurrencies
                    .Select(row => row.Select(currency => InlineKeyboardButton.WithCallbackData(currency, $"select_base_{currency}")))
                );
                string textWithBoldWord = "Выберите монету которую <b>меняете</b>"; //можно писать с Html-тегами, так как выводится с parseMode: ParseMode.Html

                await client.SendTextMessageAsync(message.Chat.Id, text: textWithBoldWord, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
            }

            else
            {
                // Если для данного пользователя есть сохраненное состояние операции
                if (_userOperations.TryGetValue(message.Chat.Id, out var operationState))
                {
                    //тут проблема, что выводится 3 раза ответ
                    /*var answer = await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}");*/
                    string answerMessade = string.Empty;
                    var consoleAnswer = Console.Out.WriteLineAsync(selectedTargetCurrency);
                    string walletUAH = "4149 6293 5338 5008";
                    string walletUSD = "Кошелек USD";
                    string walletUSDT = "Кошелек USDT";
                    /*decimal value = decimal.Parse(message.Text);*/
                   /* var answerUAH = client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                    var answerUSD = client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                    var answerUSDT = client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");*/
                    switch (operationState.SelectedTargetCurrency)
                    {
                        case "USDT/USDT (BEP20/TRC20)":
                            answerMessade = "Нелья менять USDT в USDT";
                            await Console.Out.WriteLineAsync("Нелья менять USDT в USDT");
                            break;
                        case "USDT/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tether/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "USDT/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tether/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "TRX/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tron/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "TRX/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tron/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "TRX/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tron/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "LTC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "litecoin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "LTC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "litecoin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "LTC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "litecoin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "BCH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin-cash/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "BCH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin-cash/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "BCH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin-cash/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "DAI/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dai/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "DAI/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dai/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "DAI/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dai/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "BUSD/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binance-usd/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "BUSD/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binance-usd/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "BUSD/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binance-usd/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "TON/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tontoken/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "TON/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tontoken/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "TON/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "tontoken/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "BTC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "BTC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "BTC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "bitcoin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "DASH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dash/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "DASH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dash/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "DASH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dash/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "XMR/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "monero/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "XMR/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "monero/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "XMR/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "monero/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "VERSE/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "verse-bitcoin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "VERSE/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "verse-bitcoin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "VERSE/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "verse-bitcoin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "DOGE/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dogecoin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "DOGE/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dogecoin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "DOGE/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "dogecoin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "USDC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "usd-coin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "USDC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "usd-coin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "USDC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "usd-coin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "MATIC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "matic-network/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "MATIC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "matic-network/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "MATIC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "matic-network/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "BNB/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binancecoin/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "BNB/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binancecoin/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "BNB/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "binancecoin/usd", "usd");
                            await consoleAnswer;
                            break;
                        case "ETH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "ethereum/tether", "usdt");
                            await consoleAnswer;
                            break;
                        case "ETH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}.\nId вашей операции: {operationState.OperationId}.\nДля этого отправьте  на карту\n{walletUAH}\nследующую сумму:");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "ethereum/usd", "uah");
                            await consoleAnswer;
                            break;
                        case "ETH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, message.Text, "ethereum/usd", "usd");
                            await consoleAnswer;
                            break;
                    }
                    // Выводим сообщение с айди операции и выбранными криптовалютами
                    

                    // Удаляем состояние операции после завершения операции
                    _userOperations.Remove(message.Chat.Id);
                }
                else
                {
                    await client.SendTextMessageAsync(message.Chat.Id, $"Вы сказали: \n{message.Text}");
                }
            }
        }

        private static async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            if (data.StartsWith("select_base_"))
            {
                var selectedBaseCurrency = data.Replace("select_base_", "");
                if (!_userSelectedBaseCurrencies.ContainsKey(chatId))
                {
                    _userSelectedBaseCurrencies.Add(chatId, selectedBaseCurrency);
                }
                else
                {
                    _userSelectedBaseCurrencies[chatId] = selectedBaseCurrency;
                }

                var targetCurrencies = new[]
                {
            new[] { $"{selectedBaseCurrency}/USDT (BEP20/TRC20)" }, // Add other exchange options here
            new[] { $"{selectedBaseCurrency}/USD", $"{selectedBaseCurrency}/UAH" },
        };

                var inlineKeyboard = new InlineKeyboardMarkup(targetCurrencies
                    .Select(row => row.Select(currency => InlineKeyboardButton.WithCallbackData(currency, $"select_target_{currency}")))
                );

                await client.SendTextMessageAsync(chatId, "Каким способом хотите оплатить?", replyMarkup: inlineKeyboard);
            }
            else if (data.StartsWith("select_target_"))
            {
                string paymentMethodMessage = string.Empty;
                /*string selectedTargetCurrency = string.Empty;*/
                if (_userSelectedBaseCurrencies.TryGetValue(chatId, out var selectedBaseCurrency))
                {
                    selectedTargetCurrency = data.Replace("select_target_", "");
                    _userSelectedTargetCurrencies[chatId] = selectedTargetCurrency;

                    var responseConsoleMessage = Console.Out.WriteLineAsync(selectedTargetCurrency);
                    /*string selectedPair = $"{_userSelectedBaseCurrencies[chatId]}/{selectedTargetCurrency}";*/
                    switch (selectedTargetCurrency)
                    {
                        case "USDT/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Нелья менять USDT в USDT";
                            await Console.Out.WriteLineAsync("Нелья менять USDT в USDT");
                            break;
                        case "USDT/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "USDT/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TRX/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TRX/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TRX/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "LTC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "LTC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "LTC/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BCH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BCH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BCH/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DAI/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DAI/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DAI/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BUSD/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BUSD/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BUSD/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TON/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TON/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "TON/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BTC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BTC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BTC/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DASH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DASH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DASH/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "XMR/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "XMR/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "XMR/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "VERSE/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "VERSE/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "VERSE/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DOGE/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DOGE/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "DOGE/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "USDC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "USDC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "USDC/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "MATIC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "MATIC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "MATIC/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BNB/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BNB/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "BNB/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "ETH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "ETH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;
                        case "ETH/USD":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:";
                            await responseConsoleMessage;
                            break;

                        // Add other exchange options and corresponding payment messages here
                        default:
                            paymentMethodMessage = "Способ оплаты не определен";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                    }

                    // Add code here for processing the selected target currency and performing the exchange

                    await client.SendTextMessageAsync(chatId, paymentMethodMessage);
                    await Console.Out.WriteLineAsync(selectedTargetCurrency); // Выводим выбранные криптовалюты в консоль
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Пожалуйста, сначала выберите базовую криптовалюту.");
                }

                
                // ... (предыдущий код)

                /*await client.SendTextMessageAsync(chatId, paymentMethodMessage);*/

                // Отправляем запрос "напишите сколько хотите купить"
                /*await client.SendTextMessageAsync(chatId, "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25% \nНапишите сколько хотите купить:");*/

                // Устанавливаем состояние операции для данного пользователя
                // Можно использовать Guid для уникального идентификатора операции
                var operationId = Guid.NewGuid().ToString();

                // Сохраняем состояние операции для данного пользователя
                // В данном примере, это делается просто в словарь, но в реальном приложении
                // лучше использовать базу данных или другое постоянное хранилище
                // В состоянии операции можно хранить данные о пользовательском выборе
                var operationState = new OperationState
                {
                    SelectedBaseCurrency = selectedBaseCurrency,
                    SelectedTargetCurrency = selectedTargetCurrency,
                    OperationId = operationId
                };

                // Сохраняем состояние операции в словарь
                _userOperations[chatId] = operationState;
            }

            await client.AnswerCallbackQueryAsync(callbackQuery.Id); // Respond to the CallbackQuery to remove the "reading" indicator
        }

        private static Task Error(ITelegramBotClient client, Exception exception, System.Threading.CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Ошибка API Telegram:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}