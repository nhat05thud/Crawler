using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CrawlProducts
{
    public class CrawTiki
    {
        public static readonly SqlConnection Connection = new SqlConnection("Server=NHAT-PC;Database=CrawlerTiki;User Id=sa;Password=zxc123;");
        public static readonly string TikiUrl = "https://tiki.vn";
        public static async Task MainAsync()
        {
            await StarCrawlerAsync(TikiUrl);
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
                var currentImage = htmlDocument.DocumentNode.Descendants("img").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("product-magiczoom"))?.ChildAttributes("src").FirstOrDefault()?.Value;
                var currentName = htmlDocument.DocumentNode.Descendants("h1").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("item-name"))?.InnerText.Trim();
                var currentPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("span-price"))?.InnerText.Trim();
                var currentSale = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("span-discount-percent"))?.InnerText.Trim();
                var currentRegularPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("span-list-price"))?.InnerText.Trim();
                var currentSeller = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("current-seller"))?.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("text"))?.Descendants("span").FirstOrDefault()?.InnerText.Trim();
                Connection.Open();
                try
                {
                    const string query = "INSERT INTO Product(Name,Image,Price,Sale,RegularPrice,Seller,ParentId) values (@Name,@Image,@Price,@Sale,@RegularPrice,@Seller,@ParentId)";
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = query;

                    cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                    cmd.Parameters.Add("@Image", SqlDbType.NChar).Value = currentImage;
                    cmd.Parameters.Add("@Price", SqlDbType.NChar).Value = currentPrice;
                    cmd.Parameters.Add("@Sale", SqlDbType.NChar).Value = currentSale;
                    cmd.Parameters.Add("@RegularPrice", SqlDbType.NChar).Value = currentRegularPrice;
                    cmd.Parameters.Add("@Seller", SqlDbType.Int).Value = currentSeller;
                    cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;

                    // Thực thi cmd (Dùng cho delete, insert, update).
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
                Connection.Close();
                Console.WriteLine("Add ProductName: " + currentName);
            }
        }
        public static async Task GetProducts(string url, int parentId)
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
                        currentLink = TikiUrl + product.ChildNodes[1].Attributes["href"].Value;
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
                        currentUrl = TikiUrl + nextList.Attributes["href"].Value;
                    }
                    await GetProducts(currentUrl, parentId);
                }
            }
        }
        public static async Task GetCategory(string url, string parentName, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var collapseCategory = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("collapse-category"));
            var cateTop = collapseCategory?.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("list-group-item is-top"));
            var cateCurrent = collapseCategory?.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("list-group-item is-current"));
            if (cateTop != null && cateCurrent == null || cateCurrent != null)
            {
                var childCate = collapseCategory.Descendants("div").Where(node => node.GetAttributeValue("class", "").Equals("list-group-item is-child")).ToList();
                if (parentName != childCate[0].ChildNodes[1]?.ChildNodes[1]?.ChildNodes[0]?.InnerText && parentName != "Máy Tính Xách Tay")
                {
                    foreach (var child in childCate)
                    {
                        var currentUrl = child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                        var result = Uri.TryCreate(child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                        if (result == false)
                        {
                            currentUrl = TikiUrl + child.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                        }
                        var childNodeCount = child.ChildNodes[1]?.ChildNodes.Count;
                        var currentName = string.Empty;
                        if (childNodeCount == 2)
                        {
                            currentName = child.ChildNodes[1]?.ChildNodes[0]?.InnerText;
                        }
                        if (childNodeCount == 3)
                        {
                            currentName = child.ChildNodes[1]?.ChildNodes[1]?.ChildNodes[0]?.InnerText;
                        }
                        Connection.Open();
                        int columnId;
                        try
                        {
                            const string query = "INSERT INTO ProductCategory(Name,ParentId) output inserted.Id values (@Name,@ParentId)";
                            var cmd = Connection.CreateCommand();
                            cmd.CommandText = query;

                            cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                            cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;

                            columnId = (int)cmd.ExecuteScalar();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            throw;
                        }
                        Connection.Close();
                        Console.WriteLine("Add CateName Con: " + currentName + " ----- ParentId: (" + parentId + ")");
                        await GetCategory(currentUrl, currentName, columnId);
                    }
                }
            }
            await GetProducts(url, parentId);
        }
        public static async Task StarCrawlerAsync(string url)
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
                    currentUrl = TikiUrl + link.Attributes["href"].Value;
                }
                var currentName = link.InnerText;
                Connection.Open();
                int columnId;
                try
                {
                    const string query = "INSERT INTO ProductCategory(Name,ParentId) output inserted.Id values (@Name,@ParentId)";
                    var cmd = Connection.CreateCommand();
                    cmd.CommandText = query;

                    cmd.Parameters.Add("@Name", SqlDbType.NChar).Value = currentName;
                    cmd.Parameters.Add("@ParentId", SqlDbType.Int).Value = 0;

                    columnId = (int)cmd.ExecuteScalar();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
                Connection.Close();
                Console.WriteLine("Add CateName Cha: " + currentName);
                await GetCategory(currentUrl, currentName, columnId);
            }
        }
    }
}
