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
        catch(AggregateException ex)
        {
            var only = ex.InnerExceptions.Only();
            if (only != null)
            {
                only.PreserveStackTrace();
                throw only;
            }
            else
            {
                throw;
            }
        }
    }

    public static T ResultSafe<T>(this Task<T> task)
    {
        try
        {
            return task.Result;
        }
        catch (AggregateException ex)
        {
            var only = ex.InnerExceptions.Only();
            if (only != null)
            {
                only.PreserveStackTrace();
                throw only;
            }
            else
            {
                throw;
            }
        }
    }
}
