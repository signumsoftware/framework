using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.Rest;
using Signum.Utilities;

namespace Signum.React.RestLog
{
    public class RestLogController : ApiController
    {
        [Route("api/restLog/"), HttpGet]
        public async Task<RestDiffResult> GetRestDiffLog(string id, string url)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
            var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

            var result = new RestDiffResult {previous = oldRequest.ResponseBody};
            
            //create the new Request
            var restClient = new HttpClient {BaseAddress = new Uri(url)};
            restClient.DefaultRequestHeaders.Add("X-ApiKey", oldCredentials.ApiKey);
            var request = new HttpRequestMessage(string.IsNullOrWhiteSpace(oldRequest.RequestBody) ? HttpMethod.Get : HttpMethod.Post, url);
            
            if (!string.IsNullOrWhiteSpace(oldRequest.RequestBody))
            {


                var response = await restClient.PostAsync("",new StringContent(oldRequest.RequestBody, Encoding.UTF8, "application/json"));
                result.current = await  response.Content.ReadAsStringAsync();
            }
            else
            {
                var requestUriAbsoluteUri = request.RequestUri.AbsoluteUri;
                if (requestUriAbsoluteUri.Contains("apiKey"))
                {
                    request.RequestUri = requestUriAbsoluteUri.After("apiKey=").Contains("&") ? 
                        new Uri(requestUriAbsoluteUri.Before("apiKey=") + requestUriAbsoluteUri.After("apiKey=").After('&')) : 
                        new Uri(requestUriAbsoluteUri.Before("apiKey="));
                }
                
                result.current = await restClient.SendAsync(request).Result.Content.ReadAsStringAsync();
            }

            StringDistance sd = new StringDistance();
            var diff = sd.DiffText(result.previous, result.current);
            result.diff = diff;
            return result;
        }
    }



    
}