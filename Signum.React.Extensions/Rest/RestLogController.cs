using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

            var result = new RestDiffResult {Previous = oldRequest.ResponseBody};

            //create the new Request
            var restClient = new HttpClient();
            restClient.BaseAddress = new Uri(oldRequest.Url);

            if (!string.IsNullOrWhiteSpace(oldRequest.RequestBody))
            {
                var newRequest = restClient.PostAsJsonAsync("", oldRequest.RequestBody);
                result.Current = newRequest.Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                result.Current = restClient.GetStringAsync(oldRequest.Url).Result;
            }

            StringDistance sd = new StringDistance();
            var diff = sd.DiffText(result.Previous, result.Current);
            result.diff = diff;
            return result;
        }
    }



    public class RestDiffResult
    {
        public string Previous { get; set; }
        public string Current { get; set; }
        public List<StringDistance.DiffPair<List<StringDistance.DiffPair<string>>>> diff { get; set; }
    }
}