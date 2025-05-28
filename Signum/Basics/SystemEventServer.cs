using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Signum.Basics;

public static class SystemEventServer
{
    public static void LogStartStop(IApplicationBuilder app, IHostApplicationLifetime lifetime)
    {
        SystemEventLogLogic.Log("Application Start");

        lifetime.ApplicationStopping.Register(() =>
        {
            SystemEventLogLogic.Log("Application Stop");
        });
    }
}
