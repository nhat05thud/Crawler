using System;
using System.Text;

namespace CrawlProducts
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            //CrawTiki.MainAsync().Wait();
            CrawlLazada.MainAsync().Wait();

            Console.WriteLine("Successful....");
            Console.WriteLine("Press Enter to exit the program...");
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Environment.Exit(0);
            }
        }
        
    }
}
