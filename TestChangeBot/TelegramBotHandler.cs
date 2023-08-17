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
        public enum OperationStep
        {
            SelectTargetCurrency,
            EnterAmount,
            EnterWallet
        }

        static string changePair = null;

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
            if (message?.Type == MessageType.Photo)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Спасибо, платеж отправлен в обработку. Ожидайте зачисления средств.");

                // Получаем ID фото, которое хотим переслать
                var photoId = message.Photo[0].FileId;

                // ID чата, куда нужно переслать фото (в данном случае ID технического чата)
                long technicalChatId = 6642646501; // Замените на реальный ID вашего технического чата

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
                string textWithBoldWord = "Выберите монету которую <b>покупаете</b>"; //можно писать с Html-тегами, так как выводится с parseMode: ParseMode.Html

                await client.SendTextMessageAsync(message.Chat.Id, text: textWithBoldWord, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
            }

            else
            {
                // Если для данного пользователя есть сохраненное состояние операции
                if (_userOperations.TryGetValue(message.Chat.Id, out var operationState))
                {
                    string answerMessade = string.Empty;
                    var consoleAnswer = Console.Out.WriteLineAsync(selectedTargetCurrency);
                    string walletUAH = "4149 6293 5338 5008";
                    
                    switch (operationState.CurrentStep)
                    {
                        
                        case OperationStep.EnterAmount:
                            // Код обработки суммы, например, вы можете сохранить сумму в operationState и перейти к следующему шагу:
                            operationState.Amount = message.Text;
                            operationState.CurrentStep = OperationStep.EnterWallet;

                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы хотите купить {message.Text} {selectedTargetCurrency} \nId вашей операции:{operationState.OperationId}. \nУкажите его в назначении платежа. \n\nВведите ваш кошелек для зачисления. \nИ отправьте на карту \n{walletUAH} \nследующую сумму:");
                            // Рассчитать итоговую сумму на основе выбранной криптовалюты и введенной суммы
                            await _currentCourse.CalculateAmountInUSD(message.Chat.Id, operationState.Amount, changePair, "uah");
                            operationState.OrderAmount = message.Text;
                            break;

                        case OperationStep.EnterWallet:
                            // Код обработки кошелька, например, сохранить его в operationState и вывести сообщение "Спасибо, ожидаем ваш платеж":
                            operationState.Wallet = message.Text;
                            _userOperations.Remove(message.Chat.Id); // Удаляем состояние операции после завершения операции
                            await client.SendTextMessageAsync(message.Chat.Id, "Спасибо, ожидаем ваш платеж. \nПосле отправки перешлите в бот скриншот платежа.");
                            // ID чата, куда нужно переслать фото (в данном случае ID технического чата)
                            long technicalChatId = 6642646501; // Замените на реальный ID вашего технического чата
                            string order = $"Заявка {operationState.OperationId} \nКошелек-{message.Text} \nСумма: {operationState.OrderAmount}{selectedTargetCurrency}";
                            // Пересылаем фото в технический чат
                            await client.SendTextMessageAsync(technicalChatId, $"{order}");
                            break;

                        default:
                            await client.SendTextMessageAsync(message.Chat.Id, $"Вы сказали: \n{message.Text}");
                            break;
                    }

                    // Сохраняем обновленное состояние операции
                    _userOperations[message.Chat.Id] = operationState;
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
            new[] {$"{selectedBaseCurrency}/UAH" },
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
                        case "USDT/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "tether/usd";
                            await responseConsoleMessage;
                            break;
                        case "TRX/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "tron/usd";
                            await responseConsoleMessage;
                            break;
                        case "LTC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "litecoin/usd";
                            await responseConsoleMessage;
                            break;
                        case "BCH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "bitcoin-cash/usd";
                            await responseConsoleMessage;
                            break;
                        case "DAI/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "dai/usd";
                            await responseConsoleMessage;
                            break;
                        case "BUSD/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "binance-usd/usd";
                            await responseConsoleMessage;
                            break;
                        case "TON/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "tontoken/usd";
                            await responseConsoleMessage;
                            break;
                        case "BTC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "bitcoin/usd";
                            await responseConsoleMessage;
                            break;
                        case "DASH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "dash/usd";
                            await responseConsoleMessage;
                            break;
                        case "XMR/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "monero/usd";
                            await responseConsoleMessage;
                            break;
                        case "VERSE/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "verse-bitcoin/usd";
                            await responseConsoleMessage;
                            break;
                        case "DOGE/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "dogecoin/usd";
                            await responseConsoleMessage;
                            break;
                        case "USDC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "usd-coin/usd";
                            await responseConsoleMessage;
                            break;
                        case "MATIC/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "matic-network/usd";
                            await responseConsoleMessage;
                            break;
                        case "BNB/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "binancecoin/usd";
                            await responseConsoleMessage;
                            break;
                        case "ETH/UAH":
                            paymentMethodMessage = "Комиссия при покупке: \nДо $100 - 35% \nОт $100 до $500 - 30% \nОт $500 - 25%";
                            changePair = "ethereum/usd";
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
                // Можно использовать Guid для уникального идентификатора операции
                var operationId = Guid.NewGuid().ToString();
                string orderAmount = null;
                var operationState = new OperationState
                {
                    SelectedBaseCurrency = selectedBaseCurrency,
                    SelectedTargetCurrency = selectedTargetCurrency,
                    OperationId = operationId,
                    CurrentStep = OperationStep.EnterAmount, // Переходим к следующему шагу - вводу суммы
                    OrderAmount = orderAmount,
                };

                // Сохраняем состояние операции в словарь
                _userOperations[chatId] = operationState;
                await client.SendTextMessageAsync(chatId, "Напишите сумму интересуемой валюты, которую хотите купить:");
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