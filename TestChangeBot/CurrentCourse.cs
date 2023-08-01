using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TestChangeBot
{
    public class CurrentCourse
    {
        private Dictionary<string, Dictionary<string, decimal>> _data;
        private Dictionary<string, decimal> _fiatData;
        private readonly HttpClient httpClient;

        public CurrentCourse()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task LoadCryptoCurrencyRates()
        {
            try
            {
                string url = "simple/price?ids=tether,bitcoin,ethereum,litecoin,cardano,dai,tron,bitcoin-cash,binance-usd,tontoken,dash,verse-bitcoin,dogecoin,matic-network,binancecoin,usd-coin,monero&vs_currencies=usd";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                _data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, decimal>>>(responseBody);
            }
            catch (HttpRequestException ex)
            {
                // Обработайте ошибку загрузки данных
                Console.WriteLine($"Ошибка при получении данных о курсах криптовалют: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Обработайте ошибку разбора данных
                Console.WriteLine($"Ошибка при разборе данных о курсах криптовалют: {ex.Message}");
            }
        }

        private async Task LoadFiatCurrencyRates()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://api.privatbank.ua/p24api/pubinfo?exchange&coursid=5");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<FiatCurrencyRate>>(responseBody);
                _fiatData = data.ToDictionary(rate => rate.Ccy, rate => rate.Buy);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при получении данных о курсах гривны: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка при разборе данных о курсах гривны: {ex.Message}");
            }
        }

        public async Task SendCryptoCurrencyRates(long chatId)
        {
            try
            {
                string url = "simple/price?ids=tether,bitcoin,ethereum,litecoin,cardano,dai,tron,bitcoin-cash,binance-usd,tontoken,dash,verse-bitcoin,dogecoin,matic-network,binancecoin,usd-coin,monero&vs_currencies=usd";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Dictionary<string, Dictionary<string, decimal>> data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, decimal>>>(responseBody);

                if (data.TryGetValue("tether", out Dictionary<string, decimal> tetherData) &&
                    data.TryGetValue("bitcoin", out Dictionary<string, decimal> bitcoinData) &&
                    data.TryGetValue("ethereum", out Dictionary<string, decimal> ethereumData) &&
                    data.TryGetValue("litecoin", out Dictionary<string, decimal> litecoinData) &&
                    data.TryGetValue("cardano", out Dictionary<string, decimal> cardanoData) &&
                    data.TryGetValue("dai", out Dictionary<string, decimal> daiData) &&
                    data.TryGetValue("tron", out Dictionary<string, decimal> tronData) &&
                    data.TryGetValue("bitcoin-cash", out Dictionary<string, decimal> bitcoincashData) &&
                    data.TryGetValue("binance-usd", out Dictionary<string, decimal> binanceusdData) &&
                    data.TryGetValue("tontoken", out Dictionary<string, decimal> tontokenData) &&
                    data.TryGetValue("dash", out Dictionary<string, decimal> dashData) &&
                    data.TryGetValue("verse-bitcoin", out Dictionary<string, decimal> versebitcoinData) &&
                    data.TryGetValue("dogecoin", out Dictionary<string, decimal> dogecoinData) &&
                    data.TryGetValue("matic-network", out Dictionary<string, decimal> maticnetworkData) &&
                    data.TryGetValue("binancecoin", out Dictionary<string, decimal> binancecoinData) &&
                    data.TryGetValue("usd-coin", out Dictionary<string, decimal> usdcoinData) &&
                    /*data.TryGetValue("force-bridge-usdc", out Dictionary<string, decimal> forcebridgeusdcData) &&*/
                    data.TryGetValue("monero", out Dictionary<string, decimal> moneroData))
                {
                    string rates = $"Курсы криптовалют:\n" +
                                   $"Tether (BTC): ${tetherData["usd"]}\n" +
                                   $"Биткоин (BTC): ${bitcoinData["usd"]}\n" +
                                   $"Эфириум (ETH): ${ethereumData["usd"]}\n" +
                                   $"Лайткоин (LTC): ${litecoinData["usd"]}\n" +
                                   $"Кардано (ADA): ${cardanoData["usd"]}\n" +
                                   $"Трон (TRX): ${tronData["usd"]}\n" +
                                   $"Биткоин-кеш (BCH): ${bitcoincashData["usd"]}\n" +
                                   $"Monero (XMR): ${moneroData["usd"]}\n" +
                                   $"Dai (DAI): ${daiData["usd"]}\n" +
                                   $"Binance-USD (BUSD): ${binanceusdData["usd"]}\n" +
                                   $"Ton-token (TON): ${tontokenData["usd"]}\n" +
                                   $"Dash (DASH): ${dashData["usd"]}\n" +
                                   $"Verse (VERSE): ${versebitcoinData["usd"]}\n" +
                                   $"Dogecoin (DOGE): ${dogecoinData["usd"]}\n" +
                                   $"USD Coin (USDC): ${usdcoinData["usd"]}\n" +
                                   $"Polygon (MATIC): ${maticnetworkData["usd"]}\n" +
                                   $"Binance coin (BNB): ${binancecoinData["usd"]}";
                    await TelegramBotHandler._client.SendTextMessageAsync(chatId, rates);
                }
                else
                {
                    await TelegramBotHandler._client.SendTextMessageAsync(chatId, "Не удалось получить данные о курсах криптовалют.");
                }
            }
            catch (HttpRequestException ex)
            {
                await TelegramBotHandler._client.SendTextMessageAsync(chatId, $"Ошибка при получении данных о курсах криптовалют: {ex.Message}"/*, replyMarkup: keyboard*/);
            }
            catch (JsonException ex)
            {
                await TelegramBotHandler._client.SendTextMessageAsync(chatId, $"Ошибка при разборе данных о курсах криптовалют: {ex.Message}"/*, replyMarkup: keyboard*/);
            }
        }

        private decimal GetExchangeRate(string selectedCurrencyPair)
        {
            if (_data.TryGetValue(selectedCurrencyPair.Split('/')[0].ToLower(), out var currencyData))
            {
                if (currencyData.TryGetValue("usd", out decimal exchangeRate))
                {
                    return exchangeRate;
                }
            }

            // Если валютная пара или курс обмена не найдены, можно вернуть значение по умолчанию или обработать ошибку
            return 0;
        }

        private decimal GetFiatExchangeRate(string currencyCode)
        {
            string fiatCurrencyCode = currencyCode switch
            {
                "usd" => "USD",
                "uah" => "UAH",
                // Добавьте другие коды валют по необходимости
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(fiatCurrencyCode) && _fiatData.TryGetValue(fiatCurrencyCode, out decimal exchangeRate))
            {
                return exchangeRate;
            }

            // Вернуть значение по умолчанию или обработать ошибку
            return 0;
        }

        public async Task CalculateAmountInUSD(long chatId, string message, string selectedCurrencyPair, string currenceFiat)
        {
            // Преобразуем строку message в значение типа decimal
            if (!decimal.TryParse(message.Replace(',', '.'), out decimal amountToBuy))
            {
                await TelegramBotHandler._client.SendTextMessageAsync(chatId, "Некорректный формат числа.");
                return;
            }

            // Загружаем данные о курсах криптовалют, если еще не загружены
            if (_data == null)
            {
                await LoadCryptoCurrencyRates();
            }
            if (_fiatData == null)
            {
                await LoadFiatCurrencyRates();
            }

            // Получаем текущий курс выбранной валютной пары
            decimal exchangeRate = GetExchangeRate(selectedCurrencyPair);

            decimal exchangeFiatRate = GetFiatExchangeRate(selectedCurrencyPair.Split('/')[1].ToLower());

            // Выполняем расчет
            decimal totalAmountInUSD = amountToBuy * exchangeRate;
            string totalAmountInUSDMessageUSD = $"{totalAmountInUSD}";
            decimal totalAmountInUAH = totalAmountInUSD * exchangeFiatRate;
            string totalAmountInUAHMessage = $"{totalAmountInUAH}";


            if (currenceFiat == "usd")
            {
                await TelegramBotHandler._client.SendTextMessageAsync(chatId, totalAmountInUSDMessageUSD);
            }
            if (currenceFiat == "uah")
            {
                await TelegramBotHandler._client.SendTextMessageAsync(chatId, totalAmountInUAHMessage);
            }
        }
    }
}