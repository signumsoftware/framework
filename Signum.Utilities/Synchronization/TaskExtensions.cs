using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities.Synchronization;
public static class TaskExtensions
{
    public static void WaitSafe(this Task task) => task.GetAwaiter().GetResult();
    public static T ResultSafe<T>(this Task<T> task) => task.GetAwaiter().GetResult();
}
