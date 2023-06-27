using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities.Synchronization;

public static class TaskExtensions
{
    public static void WaitSafe(this Task task)
    {
        try
        {
            task.Wait();
        }
        catch (AggregateException ag)
        {
            var ex = ag.InnerExceptions.FirstEx();
            ex.PreserveStackTrace();
            throw ex;
        }
    }

    public static TResult ResultSave<TResult>(this Task<TResult> task)
    {
        try
        {
            return task.Result;
        }
        catch (AggregateException ag)
        {
            var ex = ag.InnerExceptions.FirstEx();
            ex.PreserveStackTrace();
            throw ex;
        }
    }
}
