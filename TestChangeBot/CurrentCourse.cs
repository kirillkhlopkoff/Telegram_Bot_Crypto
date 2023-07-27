using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace TestChangeBot
{
    public class CurrentCourse
    {
        private readonly HttpClient httpClient;

        public CurrentCourse()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task SendCryptoCurrencyRates(long chatId)
        {
            try
            {
                string url = "simple/price?ids=bitcoin,ethereum,ripple&vs_currencies=usd";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Dictionary<string, Dictionary<string, decimal>> data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, decimal>>>(responseBody);

                if (data.TryGetValue("bitcoin", out Dictionary<string, decimal> bitcoinData) &&
                    data.TryGetValue("ethereum", out Dictionary<string, decimal> ethereumData) &&
                    data.TryGetValue("ripple", out Dictionary<string, decimal> rippleData))
                {
                    string rates = $"Курсы криптовалют:\n" +
                                   $"Биткоин (BTC): ${bitcoinData["usd"]}\n" +
                                   $"Эфириум (ETH): ${ethereumData["usd"]}\n" +
                                   $"Риппл (XRP): ${rippleData["usd"]}";
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
    }
}
