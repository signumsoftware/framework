using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Signum.API;

namespace Signum.Basics;

public static class SystemEventServer
{
    public static void LogStartStop(IHostApplicationLifetime lifetime)
    {
        SystemEventLogLogic.Log("Application Start");

        lifetime.ApplicationStopping.Register(() =>
        {
            SystemEventLogLogic.Log("Application Stop");
        });
    }
}
