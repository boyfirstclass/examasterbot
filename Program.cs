
using examasterbot.Logic;
using examasterbot.Storage.Csv;
using examasterbot.Tg;

namespace examasterbot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var storage = new CsvStorage("data");
            storage.LoadAll();

            const String TOKEN = "8286117551:AAEzvy52gzPTS6oyZ8dbcgAgRucytmcIqog"; 
            var logic = new BotLogic(storage);
            var botService = new TelegramBotService(TOKEN, logic);
            await botService.StartAsync();

            Console.WriteLine("Bot started. Press any key to exit.");
            Console.ReadLine();
        }
    }
}