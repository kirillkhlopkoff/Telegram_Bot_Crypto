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
            else if (message.Text == "Текущий курс")
            {
                await _currentCourse.SendCryptoCurrencyRates(message.Chat.Id);
            }
            else if (message.Text == "📞 Поддержка")
            {
                // Создаем и отправляем сообщение с ссылкой-кнопкой
                var supportButton = new InlineKeyboardButton(string.Empty)
                {
                    Text = "Связаться с поддержкой", // Указываем текст для кнопки
                    Url = "https://t.me/GrekKH" // Здесь URL, на который должна вести ссылка
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
                    /*var answer = await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}");*/
                    string answerMessade = string.Empty;
                    var consoleAnswer = Console.Out.WriteLineAsync(selectedTargetCurrency);
                    string walletUAH = "Кошелек UAH";
                    string walletUSD = "Кошелек USD";
                    string walletUSDT = "Кошелек USDT";
                    switch (operationState.SelectedTargetCurrency)
                    {
                        case "USDT/USDT (BEP20/TRC20)":
                            answerMessade = "Нелья менять USDT в USDT";
                            await Console.Out.WriteLineAsync("Нелья менять USDT в USDT");
                            break;
                        case "USDT/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "USDT/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSD}");
                            await consoleAnswer;
                            break;
                        case "TRX/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUSDT}");
                            await consoleAnswer;
                            break;
                        case "TRX/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "TRX/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "LTC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "LTC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "LTC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BCH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BCH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BCH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DAI/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DAI/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DAI/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BUSD/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BUSD/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BUSD/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "TON/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "TON/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "TON/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BTC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BTC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BTC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DASH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DASH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DASH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "XMR/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "XMR/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "XMR/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "VERSE/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "VERSE/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "VERSE/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DOGE/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DOGE/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "DOGE/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "USDC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "USDC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "USDC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "MATIC/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "MATIC/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "MATIC/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BNB/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BNB/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "BNB/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "ETH/USDT (BEP20/TRC20)":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "ETH/UAH":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
                            await consoleAnswer;
                            break;
                        case "ETH/USD":
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите поменять {message.Text} {operationState.SelectedTargetCurrency}. Id вашей операции: {operationState.OperationId}. {walletUAH}");
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

                    
                    /*string selectedPair = $"{_userSelectedBaseCurrencies[chatId]}/{selectedTargetCurrency}";*/
                    switch (selectedTargetCurrency)
                    {
                        case "USDT/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Нелья менять USDT в USDT";
                            await Console.Out.WriteLineAsync("Нелья менять USDT в USDT");
                            break;
                        case "USDT/UAH":
                            paymentMethodMessage = "Напишите сколько хотите купить:";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDT/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX/UAH":
                            paymentMethodMessage = "Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "LTC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "LTC/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "LTC/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BCH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BCH/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BCH/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DAI/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DAI/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DAI/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BUSD/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BUSD/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BUSD/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TON/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TON/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TON/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DASH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DASH/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DASH/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "XMR/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "XMR/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "XMR/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "VERSE/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "VERSE/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "VERSE/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DOGE/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DOGE/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "DOGE/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDC/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDC/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "MATIC/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "MATIC/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "MATIC/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BNB/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BNB/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BNB/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
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
                /*await client.SendTextMessageAsync(chatId, "Напишите сколько хотите купить:");*/

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