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
        [Route("api/restLog/{id}"), HttpGet]
        public RestDiffResult GetRestDiffLog(string id)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));
            var oldCredentials = Database.Query<RestApiKeyEntity>().Single(r => r.User.Is(oldRequest.User));

            var result = new RestDiffResult {previous = oldRequest.ResponseBody};

            //create the new Request
            var restClient = new HttpClient {BaseAddress = new Uri(oldRequest.Url)};
            var request = new HttpRequestMessage(string.IsNullOrWhiteSpace(oldRequest.RequestBody) ? HttpMethod.Get : HttpMethod.Post, oldRequest.Url);
            request.Headers.Add("X-ApiKey", oldCredentials.ApiKey);
            if (!string.IsNullOrWhiteSpace(oldRequest.RequestBody))
            {
                request.Content = new StringContent(oldRequest.RequestBody);
                var newRequest = restClient.SendAsync(request);
                result.current = newRequest.Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                result.current = restClient.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
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