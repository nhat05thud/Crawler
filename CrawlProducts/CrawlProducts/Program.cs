using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace CrawlProducts
{
    internal class Program
    {
        public static readonly SqlConnection Connection = new SqlConnection("Server=NHAT-PC;Database=CrawlerTiki;User Id=sa;Password=zxc123;");
        private static void Main(string[] args)
        {
            MainAsync().Wait();
            Console.WriteLine("Successful....");
            Console.WriteLine("Press Enter to exit the program...");
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Environment.Exit(0);
            }
        }
        private static async Task MainAsync()
        {
            await StarCrawlerAsync("https://tiki.vn");
        }

        private static async Task ProductDetails(string url, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            if (htmlDocument.DocumentNode.ChildNodes[1].Name == "script")
            {
                var rx = new Regex(@"(http|ftp|https)://(.*?)([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var text = htmlDocument.DocumentNode.InnerText;
                url = rx.Match(text).Value;
                await ProductDetails(url, parentId);
            }
            else
            {
                var currentImage = htmlDocument.DocumentNode.Descendants("img").FirstOrDefault(img => img.GetAttributeValue("id", "").Equals("product-magiczoom"))?.ChildAttributes("src").FirstOrDefault()?.Value;
                var currentName = htmlDocument.DocumentNode.Descendants("h1").FirstOrDefault(img => img.GetAttributeValue("class", "").Equals("item-name"))?.InnerText.Trim();
                var currentPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(img => img.GetAttributeValue("id", "").Equals("span-price"))?.InnerText.Trim();
                var currentSale = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(img => img.GetAttributeValue("id", "").Equals("span-discount-percent"))?.InnerText.Trim();
                var currentRegularPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(img => img.GetAttributeValue("class", "").Equals("span-list-price"))?.InnerText.Trim();
                var currentSeller = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(img => img.GetAttributeValue("class", "").Equals("current-seller"))?.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("text"))?.Descendants("span").FirstOrDefault()?.InnerText.Trim();
                //Connection.Open();
                //try
                //{
                //    const string query = "INSERT INTO Product(Name,Image,Price,Sale,RegularPrice,Seller,ParentId) values (@Name,@Image,@Price,@Sale,@RegularPrice,@Seller,@ParentId)";
                //    var cmd = Connection.CreateCommand();
                //    cmd.CommandText = query;

                //    cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                //    cmd.Parameters.Add("@Image", SqlDbType.NChar).Value = currentImage;
                //    cmd.Parameters.Add("@Price", SqlDbType.NChar).Value = currentPrice;
                //    cmd.Parameters.Add("@Sale", SqlDbType.NChar).Value = currentSale;
                //    cmd.Parameters.Add("@RegularPrice", SqlDbType.NChar).Value = currentRegularPrice;
                //    cmd.Parameters.Add("@Seller", SqlDbType.Int).Value = currentSeller;
                //    cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;

                //    // Thực thi cmd (Dùng cho delete, insert, update).
                //    cmd.ExecuteNonQuery();
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //    throw;
                //}
                //Connection.Close();
                Console.WriteLine("Add ProductName: " + currentName);
            }
        }
        private static async Task GetProducts(string url, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var productList = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("product-box-list"));
            if (productList != null)
            {
                var products = productList.Descendants("div").Where(node => node.GetAttributeValue("class", "").Contains("product-item")).ToList();
                foreach (var product in products)
                {
                    var currentLink = product.ChildNodes[1].Attributes["href"].Value;
                    var result = Uri.TryCreate(product.ChildNodes[1].Attributes["href"].Value, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                    if (result == false)
                    {
                        currentLink = "https://tiki.vn" + product.ChildNodes[1].Attributes["href"].Value;
                    }
                    await ProductDetails(currentLink, parentId);
                }
                var nextList = htmlDocument.DocumentNode.Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("next"));
                if (nextList != null)
                {
                    var currentUrl = nextList.Attributes["href"].Value;
                    var result = Uri.TryCreate(nextList.Attributes["href"].Value, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                    if (result == false)
                    {
                        currentUrl = "https://tiki.vn" + nextList.Attributes["href"].Value;
                    }
                    await GetProducts(currentUrl, parentId);
                }
            }
        }
        private static async Task GetCategory(string url, string parentName, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var collapseCategory = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("collapse-category"));
            var cateArrow = collapseCategory?.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("list-group-item has-arrow"));
            // lỗi
            if (cateArrow != null)
            {
                var childCate = collapseCategory.Descendants("div").Where(node => node.GetAttributeValue("class", "").Equals("list-group-item is-child")).ToList();
                foreach (var child in childCate)
                {
                    var currentUrl = child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                    var result = Uri.TryCreate(child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                    if (result == false)
                    {
                        currentUrl = "https://tiki.vn" + child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                    }
                    var currentName = child.ChildNodes[1]?.ChildNodes[0]?.InnerText;
                    //Connection.Open();
                    //int columnId;
                    //try
                    //{
                    //    const string query = "INSERT INTO ProductCategory(Name,ParentId) output inserted.Id values (@Name,@ParentId)";
                    //    var cmd = Connection.CreateCommand();
                    //    cmd.CommandText = query;

                    //    cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                    //    cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;
                        
                    //    columnId = (int)cmd.ExecuteScalar();
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e.Message);
                    //    throw;
                    //}
                    //Connection.Close();
                    Console.WriteLine("Add CateName: " + currentName);
                    await GetCategory(currentUrl, currentName, 0);
                }
            }
            await GetProducts(url, parentId);
        }
        private static async Task StarCrawlerAsync(string url)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var mainMenuLink = htmlDocument.DocumentNode.Descendants("a").Where(node => node.GetAttributeValue("class", "").Equals("MenuItem__MenuLink-tii3xq-1 efuIbv")).ToList();
            foreach (var link in mainMenuLink)
            {
                var currentUrl = link.Attributes["href"].Value;
                var result = Uri.TryCreate(link.Attributes["href"].Value, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (result == false)
                {
                    currentUrl = "https://tiki.vn" + link.Attributes["href"].Value;
                }
                var currentName = link.InnerText;
                //Connection.Open();
                //int columnId;
                //try
                //{
                //    const string query = "INSERT INTO ProductCategory(Name,ParentId) output inserted.Id values (@Name,@ParentId)";
                //    var cmd = Connection.CreateCommand();
                //    cmd.CommandText = query;

                //    cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                //    cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = 0;
                    
                //    columnId = (int)cmd.ExecuteScalar();
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //    throw;
                //}
                //Connection.Close();
                Console.WriteLine("Add CateName: " + currentName);
                await GetCategory(currentUrl, currentName, 0);
            }
        }
    }
}
