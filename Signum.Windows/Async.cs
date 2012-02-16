using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;

namespace Signum.Windows
{
    public static class Async
    {
        public static Action<Exception> ExceptionHandler;

        public static IAsyncResult Do(Action otherThread, Action endAction, Action finallyAction)
        {
            var disp = Dispatcher.CurrentDispatcher;

            Action action = () =>
            {
                try
                {
                    otherThread();
                    disp.BeginInvoke(DispatcherPriority.Normal, endAction);
                }
                catch (Exception e)
                {
                    disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ExceptionHandler(e)));
                }
                finally
                {
                    disp.BeginInvoke(DispatcherPriority.Normal, finallyAction);
                }
            };

            return action.BeginInvoke(null, null);
        }

        public static void Invoke(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        public static T Return<T>(this Dispatcher dispatcher, Func<T> func)
        {
            return (T)dispatcher.Invoke(func);
        }

        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action)
        {
            return dispatcher.BeginInvoke(action);
        }
    }
}
