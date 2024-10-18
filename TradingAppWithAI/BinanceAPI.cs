using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace TradingAppWithAI
{
    internal class BinanceAPI
    {
        private static readonly HttpClient client = new HttpClient();

        // Получение списка активов (пример для Binance)
        public static async Task<string> GetSymbolsListAsync()
        {
            var response = await client.GetStringAsync("https://api.binance.com/api/v3/ticker/price");
            return response;
        }

        // Получение текущей цены актива (пример для Binance)
        public static async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            var response = await client.GetStringAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol}");
            // Пример ответа: {"symbol":"BTCUSDT","price":"48000.00"}
            var priceData = Newtonsoft.Json.JsonConvert.DeserializeObject<PriceResponse>(response);
            return decimal.Parse(priceData.price);
        }

        class PriceResponse
        {
            public string symbol { get; set; }
            public string price { get; set; }
        }
    }
}
