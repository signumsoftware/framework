using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public async Task<RestDiffResult> GetRestDiffLog(string id, string host)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
            var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

            var result = new RestDiffResult {previous = oldRequest.ResponseBody};
            var url = host + oldRequest.Url.After("api");
            //create the new Request
            var restClient = new HttpClient {BaseAddress = new Uri(url)};
            restClient.DefaultRequestHeaders.Add("X-ApiKey", oldCredentials.ApiKey);
            var request = new HttpRequestMessage(string.IsNullOrWhiteSpace(oldRequest.RequestBody) ? HttpMethod.Get : HttpMethod.Post, url);
            
            if (!string.IsNullOrWhiteSpace(oldRequest.RequestBody))
            {


                result.current = await restClient.PostAsJsonAsync("", oldRequest.RequestBody).Result.Content
                    .ReadAsStringAsync();
            }
            else
            {
                result.current = await restClient.SendAsync(request).Result.Content.ReadAsStringAsync();
            }

            StringDistance sd = new StringDistance();
            var diff = sd.DiffText(result.previous, result.current);
            result.diff = diff;
            return result;
        }
    }



    public class RestDiffResult
    {
        public string previous { get; set; }
        public string current { get; set; }
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diff { get; set; }
    }
}