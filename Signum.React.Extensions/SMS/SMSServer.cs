using Signum.Engine.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using Signum.Entities.SMS;
using Signum.React.Json;
using Signum.Utilities;
using Signum.Engine.Basics;

namespace Signum.React.SMS
{
    public static class SMSServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            EntityJsonConverter.AfterDeserilization.Register((SMSTemplateEntity et) =>
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
