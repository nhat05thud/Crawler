using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RestSharp;

namespace CrawlProducts
{
    public class CrawlLazada
    {
        public static readonly SqlConnection Connection = new SqlConnection("Server=NHAT-PC;Database=CrawlerTiki;User Id=sa;Password=zxc123;");
        public static readonly string LazadaUrl = "https://lazada.vn/";
        public static async Task MainAsync()
        {
            await StarCrawlerAsync(LazadaUrl);
        }
        private static async Task ProductDetails(string url, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var currentImage = htmlDocument.DocumentNode.Descendants("img").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("pdp-mod-common-image gallery-preview-panel__image"))?.ChildAttributes("src").FirstOrDefault()?.Value;
            var currentName = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("pdp-mod-product-badge-title"))?.InnerText.Trim();
            var currentPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("class", "").Contains("pdp-price"))?.InnerText.Trim();
            //var currentSale = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("span-discount-percent"))?.InnerText.Trim();
            //var currentRegularPrice = htmlDocument.DocumentNode.Descendants("span").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("span-list-price"))?.InnerText.Trim();
            var currentSeller = htmlDocument.DocumentNode.Descendants("a").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("seller-name__detail-name"))?.InnerText.Trim();
            // Lưu DB
            //
            // End Lưu DB

            Console.WriteLine("Add ProductName: " + currentName);
        }
        public static async Task GetProducts(string url, int parentId, int pageIndex)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync("https:" + url);
            htmlDocument.LoadHtml(html);
            
            var productList = htmlDocument.DocumentNode.Descendants("div").FirstOrDefault(node => node.GetAttributeValue("data-qa-locator", "").Equals("general-products"));
            if (productList != null)
            {
                var products = productList.Descendants("div").Where(node => node.GetAttributeValue("data-qa-locator", "").Contains("product-item")).ToList();
                foreach (var product in products)
                {
                    var currentLink = "https:" + product.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                    await ProductDetails(currentLink, parentId);
                }
                var nextList = htmlDocument.DocumentNode.Descendants("li").FirstOrDefault(node => node.GetAttributeValue("class", "").Contains("ant-pagination-item ant-pagination-item-" + pageIndex));
                if (nextList != null)
                {
                    pageIndex = pageIndex + 1;
                    var currentUrl = url + "/?page=" + pageIndex;
                    Console.WriteLine("============== Qua List sản phẩm thứ : " + pageIndex + " ==============");
                    await GetProducts(currentUrl, parentId, pageIndex);
                }
            }
        }
        public static async Task GetSubCategory(HtmlNode node, int parentId)
        {
            var grand = node.ChildNodes.FirstOrDefault(x => x.Name.Equals("ul"));
            if (grand != null)
            {
                var grandItems = grand.ChildNodes.Where(x => x.Name.Equals("li")).ToList();
                foreach (var item in grandItems)
                {
                    var currentUrl = item.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                    var currentName = item.Descendants("span").FirstOrDefault()?.InnerText;
                    // Lưu DB
                    //
                    // End Lưu DB

                    Console.WriteLine("Add ProductCate Con: " + currentName);
                    // get products
                    await GetProducts(currentUrl, 0, 1);  // 0 = currentID
                }
            }
        }
        public static async Task GetCategory(string parentName, string htmlId, int parentId)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(LazadaUrl);
            htmlDocument.LoadHtml(html);
            var parentMenu = htmlDocument.DocumentNode.Descendants("ul").FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("lzd-site-menu-sub " + htmlId));
            if (parentMenu != null)
            {
                var childMenu = parentMenu.ChildNodes.Where(x => x.Name.Equals("li")).ToList();
                foreach (var item in childMenu)
                {
                    var currentUrl = item.Descendants("a").FirstOrDefault()?.Attributes["href"].Value;
                    var currentClass = item.ChildAttributes("class").FirstOrDefault()?.Value;
                    var currentName = item.Descendants("span").FirstOrDefault()?.InnerText;
                    // Lưu DB
                    //
                    // End Lưu DB
                    Console.WriteLine("Add ProductCate: " + currentName);
                    if (currentClass != null && currentClass.Equals("sub-item-remove-arrow"))
                    {
                        // get products
                        await GetProducts(currentUrl, 0, 1);  // 0 = currentID
                    }
                    else
                    {
                        // get subcategory
                        await GetSubCategory(item, 0); // 0 = currentID
                    }
                }
            }
        }
        public static async Task StarCrawlerAsync(string url)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            var html = await httpClient.GetStringAsync(url);
            htmlDocument.LoadHtml(html);
            var mainMenuLink = htmlDocument.DocumentNode.Descendants("li").Where(node => node.GetAttributeValue("class", "").Equals("lzd-site-menu-root-item")).ToList();
            foreach (var link in mainMenuLink)
            {
                var currentName = link.ChildNodes[1]?.ChildNodes[1]?.InnerText;
                var currentId = link.ChildAttributes("id").FirstOrDefault()?.Value;
                // Lưu DB
                //
                // End Lưu DB
                Console.WriteLine("Add CateName Cha: " + currentName);
                await GetCategory(currentName, currentId, 0); // 0 = currentID
            }
        }
    }
}
