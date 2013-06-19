using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Windows
{
    public static class Async
    {
        public static event Action<Exception, Window> AsyncUnhandledException;
        public static event Action<Exception, Window> DispatcherUnhandledException;

        [ThreadStatic]
        static Window mainWindow; 

        public static IAsyncResult Do(Action backgroundThread, Action endAction, Action finallyAction)
        {
            var disp = Dispatcher.CurrentDispatcher;
           
            Action action = () =>
            {
                try
                {
                    backgroundThread();
                    if (endAction != null)
                        disp.BeginInvoke(DispatcherPriority.Normal, endAction);
                }
                catch (Exception e)
                {
                    disp.BeginInvoke(DispatcherPriority.Normal, (Action)(() => AsyncUnhandledException(e, mainWindow)));
                }
                finally
                {
                    if (finallyAction != null)
                        disp.BeginInvoke(DispatcherPriority.Normal, finallyAction);
                }
            };

            return Task.Factory.StartNew(action);
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

        public static void ShowInAnotherThread<W>(Func<W> windowConstructor,
            Action<W> afterShown = null, EventHandler closed = null, bool avoidSpawnThread = false) where W : Window
        {
            if (avoidSpawnThread)
            {
                W win = windowConstructor();

                if (closed != null)
                    win.Closed += (sender, args) => closed(sender, args);

                win.Show();

                if (afterShown != null)
                    afterShown(win);
            }
            else
            {
                Dispatcher prevDispatcher = Dispatcher.CurrentDispatcher;

                Thread t = new Thread(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                    try
                    {
                        Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) =>
                        {
                            OnDispatcherUnhandledException(args.Exception, mainWindow);

                            args.Handled = true;
                        };

                        W win = windowConstructor();

                        mainWindow = win;

                        win.Closed += (sender, args) =>
                        {
                            ((Window)sender).Dispatcher.InvokeShutdown();
                            mainWindow = null;
                            if (closed != null)
                                closed(sender, args);
                        };

                        win.Show();

                        if (afterShown != null)
                            afterShown(win);

                        Dispatcher.Run();
                    }
                    catch (Exception e)
                    {
                        OnDispatcherUnhandledException(e, mainWindow); 
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        public static void OnDispatcherUnhandledException(Exception ex, Window win)
        {
            if (DispatcherUnhandledException == null)
                throw new InvalidOperationException("There has been an exception but Async.DispatcherUnhandledException is not set");

            DispatcherUnhandledException(ex, win);
        }

        public static void OnAsyncUnhandledException(Exception ex, Window win)
        {
            if (AsyncUnhandledException == null)
                throw new InvalidOperationException("There has been an exception but Async.AsyncUnhandledException is not set");

            AsyncUnhandledException(ex, win);
        }
    }
}
