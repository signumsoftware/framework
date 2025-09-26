using Microsoft.AspNetCore.SignalR;

namespace Signum.ConcurrentUser;

public class LogHubExceptionFilter : IHubFilter
{
    async ValueTask<object?> IHubFilter.InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch(Exception ex)
        {
            ex.LogException();
            throw;
        }
    }

    async Task IHubFilter.OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            ex.LogException();
            throw;
        }
    }

    async Task IHubFilter.OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        try
        {
            if (exception != null)
                exception.LogException();

            await next(context, exception);
        }
        catch (Exception ex)
        {
            ex.LogException();
            throw;
        }
    }
}
