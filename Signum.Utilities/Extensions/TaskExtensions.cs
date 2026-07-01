using System.Diagnostics;
using System.Threading.Tasks;

namespace Signum.Utilities;


public static class TaskExtensions
{
    [DebuggerStepThrough]
    public static async Task<R> Then<T, R>(this Task<T> task, Func<T, Task<R>> function)
    {
        var value = await task;

        try
        {
            return await function(value);
        }
        catch (Exception e)
        {

            if (value is IDisposableException de)
                de.OnException(e);

            throw;
        }
        finally
        {
            if (value != null)
            {
                // Prefer async disposal if available
                if (value is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (value is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }

    [DebuggerStepThrough]
    public static async Task<R> Then<T, R>(this Task<T> task, Func<T, R> function)
    {
        var value = await task;

        try
        {

            return function(value);
        }
        catch (Exception e)
        {

            if (value is IDisposableException de)
                de.OnException(e);

            throw;
        }
        finally
        {
            if (value != null)
            {
                // Prefer async disposal if available
                if (value is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (value is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }

    [DebuggerStepThrough]
    public static async Task Then<T>(this Task<T> task, Func<T, Task> action)
    {
        var value = await task;
        try
        {
            await action(value);
        }
        catch (Exception e)
        {

            if (value is IDisposableException de)
                de.OnException(e);

            throw;
        }
        finally
        {
            if (value != null)
            {
                // Prefer async disposal if available
                if (value is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (value is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
