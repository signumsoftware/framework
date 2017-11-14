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

namespace Signum.React.Rest
{
    public class RestLogController : ApiController
    {
        [Route("api/restLog/{id}"), HttpGet]
        public async Task<RestDiffResult> GetRestDiffLog(string id)
        {
            var oldRequest = Database.Retrieve<RestLogEntity>(PrimaryKey.Parse(id, typeof(RestLogEntity)));

            var result = new RestDiffResult {Current = oldRequest.ResponseBody};

            //create the new Request
            var restClient = new HttpClient();
            restClient.BaseAddress = new Uri(oldRequest.Url);

            if (!string.IsNullOrWhiteSpace(oldRequest.RequestBody))
            {
                var newRequest = restClient.PostAsJsonAsync("", oldRequest.RequestBody);
                result.Current = await newRequest.Result.Content.ReadAsStringAsync();
            }
            else
            {
                result.Current = restClient.GetStringAsync(oldRequest.Url).Result;
            }
                

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