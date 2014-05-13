using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using Signum.Utilities;
using System.Collections.Concurrent;
using System.Globalization;

namespace Signum.Windows
{
    public static class Async
    {
        public static event Action<Exception, Window> AsyncUnhandledException;
        public static event Action<Exception, Window> DispatcherUnhandledException;

        static ConcurrentDictionary<Thread, Window> threadWindows = new ConcurrentDictionary<Thread, Window>();

        public static IAsyncResult Do(Action backgroundThread, Action endAction, Action finallyAction)
        {
            var disp = Dispatcher.CurrentDispatcher;
           
            Action action = () =>
            {
                try
                {
                    backgroundThread();
                    if (endAction != null)
                        disp.Invoke(DispatcherPriority.Normal, endAction);
                }
                catch (Exception e)
                {
                    disp.Invoke(DispatcherPriority.Normal, (Action)(() => AsyncUnhandledException(e, threadWindows.TryGetC(Thread.CurrentThread))));
                }
                finally
                {
                    if (finallyAction != null)
                        disp.Invoke(DispatcherPriority.Normal, finallyAction);
                }
            };

            return Task.Factory.StartNew(action);
        }

        //Remove on .net 4.5
        public static void AssignCultures(this Thread current, Thread parent)
        {
            current.CurrentCulture = parent.CurrentCulture;
            current.CurrentUICulture = parent.CurrentUICulture;
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

                var parent = Thread.CurrentThread;

                Thread t = new Thread(() =>
                {
                    Thread.CurrentThread.AssignCultures(parent);

                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                    try
                    {
                        Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) =>
                        {
                            OnDispatcherUnhandledException(args.Exception, threadWindows.TryGetC(Thread.CurrentThread));

                            args.Handled = true;
                        };

                        W win = windowConstructor();

                        threadWindows.TryAdd(Thread.CurrentThread, win);

                        win.Closed += (sender, args) =>
                        {
                            if (closed != null)
                                closed(sender, args);

                            ((Window)sender).Dispatcher.InvokeShutdown();
                            Window rubish;
                            threadWindows.TryRemove(Thread.CurrentThread, out rubish);
                        };

                        win.Show();

                        if (afterShown != null)
                            afterShown(win);

                        Dispatcher.Run();
                    }
                    catch (Exception e)
                    {
                        OnDispatcherUnhandledException(e, threadWindows.TryGetC(Thread.CurrentThread)); 
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

        public static bool CloseAllThreadWindows()
        {
            foreach (var win in threadWindows.Values.ToList())
            {
                win.Dispatcher.Invoke(() => win.Close());
            }

            return threadWindows.Any();
        }

        internal static System.Windows.Controls.Control GetCurrentWindow()
        {
            var win = threadWindows.TryGetC(Thread.CurrentThread);

            if (win == null)
                return null;
            
            return win;
        }
    }
}
