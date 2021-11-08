using Signum.Engine.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Signum.Entities.SMS;
using Signum.React.Json;
using Signum.React.Facades;

namespace Signum.React.SMS
{
    public static class SMSServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((SMSTemplateEntity et) =>
            {
                if (et.Query != null)
                {
                    var qd = QueryLogic.Queries.QueryDescription(et.Query.ToQueryName());
                    et.ParseData(qd);
                }
            });
        }
    }
}
