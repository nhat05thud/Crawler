using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;

namespace CrawlProducts
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            Console.Write("Insert link: ");
            var link = Console.ReadLine();
            await StarCrawlerAsync(link);
            Console.WriteLine();
        }

        private static async Task StarCrawlerAsync(string url)
        {
            // the url of the page we want to test
            //const string url = "https://www.automobile.tn/fr/neuf/bmw";
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // a list to add all the list of cars and the various prices
            var cars = new List<Car>();

            var divs = htmlDocument.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "").Equals("versions-item")).ToList();
            foreach (var div in divs)
            {
                var car = new Car
                {
                    Model = div.Descendants("h2").FirstOrDefault()?.InnerText,
                    Price = div.Descendants("div").FirstOrDefault(x=>x.GetAttributeValue("class","").Equals("price"))?.ChildNodes[1]?.InnerText,
                    Link = div.Descendants("a").FirstOrDefault()?.ChildAttributes("href").FirstOrDefault()?.Value,
                    ImageUrl = div.Descendants("img").FirstOrDefault()?.ChildAttributes("src").FirstOrDefault()?.Value
                };
                cars.Add(car);
            }

            // Connection string
            const string myConnection = "Server=NHAT-PC;Database=CrawlerTest;User Id=sa;Password=zxc123;";
            var con = new SqlConnection(myConnection);
            con.Open();
            try
            {
                foreach (var item in cars)
                {
                    const string query = "INSERT INTO Car(Model,Price,Link,ImageUrl)" + "values (@Model,@Price,@Link,@ImageUrl)";
                    var cmd = con.CreateCommand();
                    cmd.CommandText = query;

                    cmd.Parameters.Add("@Model", SqlDbType.NChar).Value = item.Model;
                    cmd.Parameters.Add("@Price", SqlDbType.NChar).Value = item.Price;
                    cmd.Parameters.Add("@Link", SqlDbType.NChar).Value = item.Link;
                    cmd.Parameters.Add("@ImageUrl", SqlDbType.NChar).Value = item.ImageUrl;

                    // Thực thi cmd (Dùng cho delete, insert, update).
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            con.Close();
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
