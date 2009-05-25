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
        public static Action<Exception, Window> ExceptionHandler;

        public static IAsyncResult Do(Window win, Action otherThread, Action endAction, Action finallyAction)
        {          
            Action async = () =>
            {
                try
                {
                    otherThread();
                    win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, endAction);
                }
                catch (Exception e)
                {
                    win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => ExceptionHandler(e, win)));
                }
                finally
                {
                    win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, finallyAction);
                }
            };

            return async.BeginInvoke(null, null);
        }

        public static void InvokeAction(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action, new object[0]);
        }
    }
}
