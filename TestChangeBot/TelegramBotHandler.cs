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
                    new KeyboardButton[] { new KeyboardButton("Текущий курс") },
                    new KeyboardButton[] { new KeyboardButton("Приобрести криптовалюту") }
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

            else if (message.Text == "Приобрести криптовалюту")
            {
                var cryptoCurrencies = new[]
                {
        new[] { "BNB (Smart Chain Bep20)", "Matic (Polygon)" }, // Здесь можно добавить другие криптовалюты
        new[] { "SOL (Solana)", "TRX (Tron TRC20)" },
        new[] { "ETH (ERC20/Arb/OP/ZK)", "USDT (BEP20/TRC20)" },
        new[] { "APT (Aptos)", "SUI (Sui Network)" },
        new[] { "BTC (Bitcoin)", "BRC (Beercoin)" }
    };

                var inlineKeyboard = new InlineKeyboardMarkup(cryptoCurrencies
                    .Select(row => row.Select(currency => InlineKeyboardButton.WithCallbackData(currency, $"select_base_{currency}")))
                );

                await client.SendTextMessageAsync(message.Chat.Id, "Выберите криптовалюту:", replyMarkup: inlineKeyboard);
            }

            else
            {
                await client.SendTextMessageAsync(message.Chat.Id, $"Вы сказали: \n{message.Text}");
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
                if (_userSelectedBaseCurrencies.TryGetValue(chatId, out var selectedBaseCurrency))
                {
                    var selectedTargetCurrency = data.Replace("select_target_", "");
                    _userSelectedTargetCurrencies[chatId] = selectedTargetCurrency;

                    string paymentMethodMessage = string.Empty;
                    /*string selectedPair = $"{_userSelectedBaseCurrencies[chatId]}/{selectedTargetCurrency}";*/
                    switch (selectedTargetCurrency)
                    {
                        case "BNB (Smart Chain Bep20)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BNB (Smart Chain Bep20)/UAH":
                            paymentMethodMessage = "Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BNB (Smart Chain Bep20)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "Matic (Polygon)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "Matic (Polygon)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "Matic (Polygon)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SOL (Solana)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SOL (Solana)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SOL (Solana)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX (Tron TRC20)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX (Tron TRC20)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "TRX (Tron TRC20)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH (ERC20/Arb/OP/ZK)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH (ERC20/Arb/OP/ZK)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "ETH (ERC20/Arb/OP/ZK)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDT (BEP20/TRC20)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = "Нелья менять USDT в USDT";
                            await Console.Out.WriteLineAsync("Нелья менять USDT в USDT");
                            break;
                        case "USDT (BEP20/TRC20)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "USDT (BEP20/TRC20)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "APT (Aptos)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "APT (Aptos)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "APT (Aptos)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SUI (Sui Network)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SUI (Sui Network)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "SUI (Sui Network)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC (Bitcoin)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC (Bitcoin)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BTC (Bitcoin)/USD":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USD\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BRC (Beercoin)/USDT (BEP20/TRC20)":
                            paymentMethodMessage = $"Вот кошелек для оплаты в USDT\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BRC (Beercoin)/UAH":
                            paymentMethodMessage = $"Вот карта для оплаты в UAH: 5375414127082617\n В назначении платежа укажите: {selectedTargetCurrency}";
                            await Console.Out.WriteLineAsync(selectedTargetCurrency);
                            break;
                        case "BRC (Beercoin)/USD":
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