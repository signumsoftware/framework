using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Threading;
using Signum.Utilities;
using System.IO;

namespace Signum.Engine
{
    public static class ProgressExtensions
    {
        public static void ProgressForeach<T>(this IEnumerable<T> collection, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            using (StreamWriter log = TryOpenAutoFlush(fileName))
            {
                if (log != null)
                    log.AutoFlush = true;

                LogWriter writer = GetLogWriter(log);

                IProgressInfo pi;

                foreach (var item in collection.ToProgressEnumerator(out pi))
                {
                    try
                    {
                        using (Transaction tr = new Transaction())
                        {
                            action(item, writer);
                            tr.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        writer(ConsoleColor.Red, "Error in {0}: {1}", elementID(item), e.Message);
                        writer(ConsoleColor.DarkRed, e.StackTrace.Indent(4));
                    }

                    SafeConsole.WriteSameLine(pi.ToString());
                }
            }
        }

        private static StreamWriter TryOpenAutoFlush(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            var result = File.CreateText(fileName);
            result.AutoFlush = true;
            return result;
        }

        private static LogWriter GetLogWriter(StreamWriter logStreamWriter)
        {
            if (logStreamWriter != null)
            {
                return (color, str, parameters) =>
                {
                    string f = parameters == null ? str : str.Formato(parameters);
                    lock (logStreamWriter)
                        logStreamWriter.WriteLine(f);
                    SafeConsole.WriteLineColor(color, f.PadRight(Console.WindowWidth - 4));
                };
            }
            else
            {
                return (color, str, parameters) =>
                    SafeConsole.WriteLineColor(color, str.Formato(parameters).PadRight(Console.WindowWidth - 4));
            }
        }

        public static void ProgressForeachDisableIdentity<T>(this IEnumerable<T> collection, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            Table table = Schema.Current.Table(tableType);
            table.Identity = false;
            try
            {
                collection.ProgressForeach(elementID, fileName, (item, writer) =>
                {
                    using (Transaction tr = new Transaction())
                    {
                        using (Administrator.DisableIdentity(table.Name))
                            action(item, writer);
                        tr.Commit();
                    }
                });
            }
            finally
            {
                table.Identity = false;
            }
        }

        public static void ProgressForeachDisableIdentity<T>(this IEnumerable<T> collection, bool isParallel, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            Table table = Schema.Current.Table(tableType);
            table.Identity = false;
            try
            {
                if (isParallel)
                    collection.ProgressForeachParallel(elementID, fileName, (item, writer) =>
                    {
                        using (Transaction tr = new Transaction())
                        {
                            using (Administrator.DisableIdentity(table.Name))
                                action(item, writer);
                            tr.Commit();
                        }
                    });
                else
                    collection.ProgressForeach(elementID, fileName, (item, writer) =>
                    {
                        using (Transaction tr = new Transaction())
                        {
                            using (Administrator.DisableIdentity(table.Name))
                                action(item, writer);
                            tr.Commit();
                        }
                    });
            }
            finally
            {
                table.Identity = false;
            }
        }

        public static void ProgressForeachParallel<T>(this IEnumerable<T> collection, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            using (StreamWriter log = TryOpenAutoFlush(fileName))
            {
                LogWriter writer = GetLogWriter(log);

                IProgressInfo pi;
                var col = collection.ToProgressEnumerator(out pi).AsThreadSafe();

                List<Thread> t = 0.To(4).Select(i => new Thread(() =>
                {
                    foreach (var item in col)
                    {
                        try
                        {
                            using (Transaction tr = new Transaction())
                            {
                                action(item, writer);
                                tr.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            writer(ConsoleColor.Red, "Error in {0}: {1}", elementID(item), e.Message);
                        }

                        SafeConsole.WriteSameLine(pi.ToString());
                    }
                })).ToList();

                t.ForEach(a => a.Start());

                t.ForEach(a => a.Join());
            }
        }

        public static void ProgressForeachParallelDisableIdentity<T>(this IEnumerable<T> collection, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            Table table = Schema.Current.Table(tableType);
            table.Identity = false;
            try
            {
                collection.ProgressForeachParallel(elementID, fileName, (item, writer) =>
                {
                    using (Transaction tr = new Transaction())
                    {
                        using (Administrator.DisableIdentity(table.Name))
                            action(item, writer);
                        tr.Commit();
                    }
                });
            }
            finally
            {
                table.Identity = false;
            }
        }


        public delegate void LogWriter(ConsoleColor color, string text, params object[] parameters);

    }
}
