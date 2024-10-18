using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingAppWithAI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Добро пожаловать в торгового бота!");

            // Получение списка активов
            Console.WriteLine("Загрузка доступных активов...");
            var symbols = await TradingBot.GetSymbolsListAsync();

            if (symbols == null || symbols.Count == 0)
            {
                Console.WriteLine("Не удалось загрузить список активов. Попробуйте позже.");
                return;
            }

            Console.WriteLine("Доступные активы:");

            foreach (var symbol in symbols.Take(15))  // Отобразим первые 15 активов
            {
                decimal currentPrice = await TradingBot.GetCurrentPriceAsync(symbol);
                Console.WriteLine($"{symbol}: {currentPrice} USD");
            }

            // Выбор актива
            string selectedSymbol;
            do
            {
                Console.Write("Выберите актив для торговли (например, BTCUSDT): ");
                selectedSymbol = Console.ReadLine();

                if (!symbols.Contains(selectedSymbol.ToUpper()))
                {
                    Console.WriteLine("Актив введен неверно или не существует. Попробуйте снова.");
                    selectedSymbol = null;
                }
            } while (string.IsNullOrEmpty(selectedSymbol));

            // Установка параметров стратегии
            decimal percentage;
            do
            {
                Console.Write("Введите процент для сеточной стратегии (например, 1%): ");
                string input = Console.ReadLine();

                // Попытка преобразования строки в число с проверкой
                if (!decimal.TryParse(input, out percentage) || percentage <= 0 || percentage > 100)
                {
                    Console.WriteLine("Неверный ввод. Убедитесь, что процент больше 0 и меньше или равен 100.");
                    percentage = 0;
                }
            } while (percentage <= 0 || percentage > 100);

            // Запуск торговли
            TradingBot bot = new TradingBot();
            await bot.StartTradingAsync(selectedSymbol, percentage);
        }
    }
}
