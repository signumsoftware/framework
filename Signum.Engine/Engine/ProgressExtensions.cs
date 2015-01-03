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
    public delegate bool StopOnExceptionDelegate(string id, string fileName, Exception exception);

    public static class ProgressExtensions
    {
        public static StopOnExceptionDelegate StopOnException = null;

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
                    using (HeavyProfiler.Log("ProgressForeach", () => elementID(item)))
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
                            writer(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                            writer(ConsoleColor.DarkRed, e.StackTrace.Indent(4));

                            if (StopOnException != null && StopOnException(elementID(item), fileName, e))
                                throw; 
                        }

                    SafeConsole.WriteSameLine(pi.ToString());
                }
                SafeConsole.ClearSameLine();
            }
        }

        public static void ProgressForeachParallel<T>(this IEnumerable<T> collection, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            using (StreamWriter log = TryOpenAutoFlush(fileName))
            {
                try
                {
                    LogWriter writer = GetLogWriter(log);

                    IProgressInfo pi;
                    var col = collection.ToProgressEnumerator(out pi).AsThreadSafe();
                    Exception stopException = null; 
                    List<Thread> t = 0.To(4).Select(i => new Thread(() =>
                    {
                        foreach (var item in col)
                        {
                            using (HeavyProfiler.Log("ProgressForeach", () => elementID(item)))
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
                                    writer(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                                    writer(ConsoleColor.DarkRed, e.StackTrace.Indent(4));

                                    if (StopOnException != null && StopOnException(elementID(item), fileName, e))
                                        stopException = e;
                                }
                            lock (SafeConsole.SyncKey)
                                SafeConsole.WriteSameLine(pi.ToString());

                            if (stopException != null)
                                break;
                        }
                    })).ToList();

                    t.ForEach(a => a.Start());

                    t.ForEach(a => a.Join());

                    if (stopException != null)
                        throw stopException;
                }
                finally
                {
                    SafeConsole.ClearSameLine();
                }
            }
        }

        public static void ProgressForeach<T>(this IEnumerable<T> collection, bool isParallel, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            if (isParallel)
                collection.ProgressForeachParallel(elementID, fileName, action);
            else
                collection.ProgressForeach(elementID, fileName, action);
        }

        public static void ProgressForeachDisableIdentity<T>(this IEnumerable<T> collection, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            Table table = Schema.Current.Table(tableType);

            if (!table.IdentityBehaviour)
                throw new InvalidOperationException("Identity is false already");

            table.IdentityBehaviour = false;
            try
            {
                collection.ProgressForeach(elementID, fileName, (item, writer) =>
                {
                    using (table.PrimaryKey.Default != null ? null: Administrator.DisableIdentity(table.Name))
                        action(item, writer);
                });
            }
            finally
            {
                table.IdentityBehaviour = true;
                SafeConsole.ClearSameLine();
            }
        }

       

        public static void ProgressForeachParallelDisableIdentity<T>(this IEnumerable<T> collection, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            Table table = Schema.Current.Table(tableType);

            if (!table.IdentityBehaviour)
                throw new InvalidOperationException("Identity is false already");

            table.IdentityBehaviour = false;
            try
            {
                collection.ProgressForeachParallel(elementID, fileName, (item, writer) =>
                {
                    using (table.PrimaryKey.Default != null ? null : Administrator.DisableIdentity(table.Name))
                        action(item, writer);
                });
            }
            finally
            {
                table.IdentityBehaviour = true;
            }
        }

        public static void ProgressForeachDisableIdentity<T>(this IEnumerable<T> collection, bool isParallel, Type tableType, Func<T, string> elementID, string fileName, Action<T, LogWriter> action)
        {
            if (isParallel)
                collection.ProgressForeachParallelDisableIdentity(tableType, elementID, fileName, action);
            else
                collection.ProgressForeachDisableIdentity(tableType, elementID, fileName, action);
        }

        public static string DefaultLogFolder = "Log";

        private static StreamWriter TryOpenAutoFlush(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (!Path.IsPathRooted(fileName))
            {
                if (!Directory.Exists(DefaultLogFolder))
                    Directory.CreateDirectory(DefaultLogFolder);

                fileName = Path.Combine(DefaultLogFolder, fileName);
            }

            var result = new StreamWriter(fileName, append: true);
            result.AutoFlush = true;
            return result;
        }

        private static LogWriter GetLogWriter(StreamWriter logStreamWriter)
        {
            if (logStreamWriter != null)
            {
                return (color, str, parameters) =>
                {
                    string f = parameters == null ? str : str.FormatWith(parameters);
                    lock (logStreamWriter)
                        logStreamWriter.WriteLine(f);
                    lock (SafeConsole.SyncKey)
                    {
                        SafeConsole.ClearSameLine();
                        SafeConsole.WriteLineColor(color, str, parameters);
                    }
                };
            }
            else
            {
                return (color, str, parameters) =>
                {
                    lock (SafeConsole.SyncKey)
                    {
                        SafeConsole.ClearSameLine();
                        SafeConsole.WriteLineColor(color, str, parameters);
                    }
                };
            }
        }

        public delegate void LogWriter(ConsoleColor color, string text, params object[] parameters);

    }
}
