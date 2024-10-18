using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Globalization;

namespace TradingAppWithAI
{
    class TradingBot
    {
        private decimal capital = 100m; // Начальный капитал
        private decimal unitSize;         // Размер одной позиции
        private List<decimal> buyPrices = new List<decimal>();
        private List<decimal> sellPrices = new List<decimal>();
        private List<bool> bought = new List<bool>(); // Список для отслеживания покупок
        private List<decimal> ownedAssets = new List<decimal>(); // Храним количество купленных активов
        private static readonly HttpClient client = new HttpClient();

        public TradingBot()
        {
            unitSize = capital / 10m;  // Делим капитал на 10 частей
        }

        public static async Task<List<string>> GetSymbolsListAsync()
        {
            var response = await client.GetStringAsync("https://api.binance.com/api/v3/exchangeInfo");
            var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfoResponse>(response);

            List<string> symbols = new List<string>();
            foreach (var symbolInfo in exchangeInfo.symbols)
            {
                if (symbolInfo.quoteAsset == "USDT")
                {
                    symbols.Add(symbolInfo.symbol);
                }
            }

            return symbols;
        }

        public static async Task<List<(string Symbol, decimal Price)>> GetSymbolsWithPricesAsync()
        {
            var response = await client.GetStringAsync("https://api.binance.com/api/v3/exchangeInfo");
            var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfoResponse>(response);

            List<(string Symbol, decimal Price)> symbolsWithPrices = new List<(string, decimal)>();
            foreach (var symbolInfo in exchangeInfo.symbols)
            {
                if (symbolInfo.quoteAsset == "USDT")
                {
                    decimal price = await GetCurrentPriceAsync(symbolInfo.symbol);
                    symbolsWithPrices.Add((symbolInfo.symbol, price));
                }
            }

            return symbolsWithPrices;
        }

        public static async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            var response = await client.GetStringAsync($"https://api.binance.com/api/v3/ticker/price?symbol={symbol}");
            var priceData = JsonConvert.DeserializeObject<PriceResponse>(response);

            if (decimal.TryParse(priceData.price, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
            {
                return price;
            }
            else
            {
                throw new FormatException($"Не удалось преобразовать строку '{priceData.price}' в число.");
            }
        }

        // Установка стратегии: процент для покупки и продажи
        public async Task StartTradingAsync(string symbol, decimal percentage)
        {
            Console.WriteLine("Запуск торговли...");

            decimal currentPrice = await GetCurrentPriceAsync(symbol);
            Console.WriteLine($"Текущая цена {symbol}: {currentPrice} USD");

            for (int i = 0; i < 10; i++)
            {
                decimal buyPrice = currentPrice * (1 - (percentage / 100m));
                decimal sellPrice = buyPrice * (1 + (percentage / 100m));

                buyPrices.Add(buyPrice);
                sellPrices.Add(sellPrice);
                bought.Add(false); // Устанавливаем, что актив не куплен
                ownedAssets.Add(0); // Инициализируем количество активов

                Console.WriteLine($"Ордер на покупку №{i + 1} по цене: {buyPrice} USD");
                Console.WriteLine($"Ордер на продажу №{i + 1} по цене: {sellPrice} USD");
            }

            // Логика торговли: проверка условий покупки/продажи в цикле
            while (true)
            {
                currentPrice = await GetCurrentPriceAsync(symbol);
                Console.WriteLine($"Текущая цена {symbol}: {currentPrice} USD");

                for (int i = 0; i < buyPrices.Count; i++)
                {
                    // Проверяем, можем ли мы купить актив
                    if (!bought[i] && currentPrice <= buyPrices[i])
                    {
                        // Количество активов, которое можем купить
                        decimal amountToBuy = Math.Floor(unitSize / currentPrice); // Можно купить только целое количество активов

                        if (amountToBuy > 0 && (capital >= amountToBuy * currentPrice)) // Убедимся, что у нас достаточно капитала
                        {
                            Console.WriteLine($"Покупка {amountToBuy} по цене: {buyPrices[i]} USD");
                            capital -= amountToBuy * currentPrice; // Уменьшаем капитал
                            ownedAssets[i] += amountToBuy; // Увеличиваем количество купленных активов
                            bought[i] = true; // Отмечаем, что актив куплен
                        }
                    }

                    // Продажа происходит только если актив был куплен и текущая цена >= ордера на продажу
                    if (bought[i] && currentPrice >= sellPrices[i])
                    {
                        if (ownedAssets[i] > 0)
                        {
                            Console.WriteLine($"Продажа {ownedAssets[i]} по цене: {sellPrices[i]} USD");
                            capital += ownedAssets[i] * currentPrice; // Возвращаем капитал
                            ownedAssets[i] = 0; // Сбрасываем количество активов
                            bought[i] = false; // Отмечаем, что актив продан
                        }
                    }
                }

                Console.WriteLine($"Текущий капитал: {capital} USD");
                await Task.Delay(5000);  // Задержка для обновления данных
            }
        }
    }

    class PriceResponse
    {
        public string symbol { get; set; }
        public string price { get; set; }
    }

    class ExchangeInfoResponse
    {
        public List<SymbolInfo> symbols { get; set; }
    }

    class SymbolInfo
    {
        public string symbol { get; set; }
        public string quoteAsset { get; set; }
    }
}
