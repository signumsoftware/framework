using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Engine.Translation
{
    //https://msdn.microsoft.com/en-us/library/ff512422.aspx
    public class AzureTranslator : ITranslator
    {
        public string AzureKey;
        public string? Proxy { get; }

        public AzureTranslator(string azureKey, string? proxy = null)
        {
            this.AzureKey = azureKey;
            this.Proxy = proxy;
        }


        static readonly XNamespace Ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
        static readonly XNamespace ArrayNs = XNamespace.Get("http://schemas.microsoft.com/2003/10/Serialization/Arrays");
        

        public async Task<List<string?>> TranslateBatchAsync(List<string> list, string from, string to)
        {
            string authToken = await AzureAccessToken.GetAccessTokenAsync(AzureKey, Proxy);
            
            var body =
                new XElement("TranslateArrayRequest",
                    new XElement("AppId"),
                    new XElement("From", from),
                    new XElement("Options",
                        new XElement(Ns + "ContentType", "text/html")
                    ),
                    new XElement("Texts",
                        list.Select(str => new XElement(ArrayNs + "string", str))
                    ),
                    new XElement("To", to)
                );

            using (var client = ExtendedHttpClient.GetClientWithProxy(Proxy))
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri("https://api.microsofttranslator.com/v2/Http.svc/TranslateArray");
                request.Content = new StringContent(body.ToString(), Encoding.UTF8, "text/xml");
                request.Headers.Add("Authorization", authToken);

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                XDocument doc = XDocument.Parse(responseBody);
                var result = doc.Descendants(Ns + "TranslateArrayResponse").Select(r => (string?)r.Element(Ns + "TranslatedText").Value).ToList();
                return result;
            }
        }

        public List<string?> TranslateBatch(List<string> list, string from, string to)
        {
            var result = Task.Run<List<string?>>(async () =>
            {
                return await this.TranslateBatchAsync(list, from, to);
            }).Result;
            
            return result;
        }

        public bool AutoSelect() => true;
    }

    public static class AzureAccessToken
    {
        public static async Task<string> GetAccessTokenAsync(string subscriptionKey, string? proxy)
        {
            using (var client = ExtendedHttpClient.GetClientWithProxy(proxy))
            using (var request = new HttpRequestMessage())
            {

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                return "Bearer " + token;
            }
        }
    }

    public static class ExtendedHttpClient
    {
        public static HttpClient GetClientWithProxy(string? proxy)
        {
            HttpClient client;
            if (!String.IsNullOrEmpty(proxy))
            {
                HttpClientHandler handler = new HttpClientHandler() { Proxy = new WebProxy() { Address = new Uri(proxy) } };
                client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
            }
            else
            {
                client = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
            }

            return client;
        }
    }
}
