using Signum.Engine.Maps;
using System.IO;

namespace Signum.Engine;


public class StopProgressForeachException : Exception
{
    public StopProgressForeachException() { }
    public StopProgressForeachException(string message) : base(message) { }
    public StopProgressForeachException(string message, Exception inner) : base(message, inner) { }
}

public delegate bool StopOnExceptionDelegate(string id, Exception exception);

public static class ProgressExtensions
{
    public static StopOnExceptionDelegate StopOnException = (id, exception) => exception is StopProgressForeachException;

    public static List<R> ProgressSelect<T, R>(this IEnumerable<T> collection,
        Func<T, R> selector,
        Func<T, string>? elementID = null,
        LogWriter? writer = null,
        bool showProgress = true,
        ParallelOptions? parallelOptions = null)
    {
        List<R> result = new List<R>();

        collection.ProgressForeach(
            action: a => result.Add(selector(a)),
            elementID: elementID,
            transactional: false,
            showProgress: showProgress,
            writer: writer,
            parallelOptions: parallelOptions);

        return result;
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
    {
        int idx = 0;
        foreach (T item in enumerable)
            handler(item, idx++);
    }

    /// <summary>
    /// Executes an action for each element in the collection transactionally and showing the progress in the Console
    /// </summary>
    public static void ProgressForeach<T>(this IEnumerable<T> collection,
        Func<T, string>? elementID = null,
        Action<T>? action = null,
        bool transactional = true,
        bool showProgress = true,
        bool stopOnException = false,
        LogWriter? writer = null,
        ParallelOptions? parallelOptions = null,
        Type? disableIdentityFor = null)
    {
        if (action == null)
            throw new InvalidOperationException("no action specified");

        if (elementID == null)
        {
            elementID = e => e!.ToString()!;
        }
        if (writer == null)
            writer = GetConsoleWriter();


        if (disableIdentityFor == null)
        {
            collection.ProgressForeachInternal(elementID, writer, parallelOptions, transactional, showProgress, stopOnException, action);
        }
        else
        {
            if (!transactional)
                throw new InvalidOperationException("disableIdentity has to be transactional");

            Table table = Schema.Current.Table(disableIdentityFor);

            collection.ProgressForeachInternal(elementID, writer, parallelOptions, transactional, showProgress, stopOnException, action: item =>
            {
                using (var tr = Transaction.ForceNew())
                {
                    using (table.PrimaryKey.Default != null ? null : Administrator.DisableIdentity(table))
                        action!(item);
                    tr.Commit();
                }
            });
        }
    }

    private static void ProgressForeachInternal<T>(this IEnumerable<T> collection,
        Func<T, string> elementID,
        LogWriter writer,
        ParallelOptions? parallelOptions,
        bool transactional,
        bool showProgress,
        bool stopOnException,
        Action<T> action
    )
    {
        if (parallelOptions != null)
            collection.ProgressForeachParallel(elementID, writer, parallelOptions, transactional, showProgress, stopOnException, action);
        else
            collection.ProgressForeachSequential(elementID, writer, transactional, showProgress, stopOnException, action);
    }

    private static void ProgressForeachSequential<T>(this IEnumerable<T> collection,
        Func<T, string> elementID,
        LogWriter writer,
        bool transactional,
        bool showProgress,
        bool stopOnException,
        Action<T> action)
    {
        var enumerator = collection.ToProgressEnumerator(out IProgressInfo pi);

        if (!Console.IsOutputRedirected && showProgress)
            SafeConsole.WriteSameLine(pi.ToString());

        foreach (var item in enumerator)
        {
            using (HeavyProfiler.Log("ProgressForeach", () => elementID(item)))
                try
                {
                    if (transactional)
                    {
                        using (var tr = Transaction.ForceNew())
                        {
                            action(item);
                            tr.Commit();
                        }
                    }
                    else
                    {
                        action(item);
                    }
                }
                catch (Exception e)
                {
                    writer(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                    writer(ConsoleColor.DarkRed, e.StackTrace!.Indent(4));

                    if (stopOnException || StopOnException != null && StopOnException(elementID(item), e))
                        throw;
                }

            if (!Console.IsOutputRedirected && showProgress)
                SafeConsole.WriteSameLine(pi.ToString());
        }
        if (!Console.IsOutputRedirected && showProgress)
            SafeConsole.ClearSameLine();

    }



    /// <summary>
    /// Executes an action for each element in the collection in paralel and transactionally, and showing the progress in the Console.
    /// <param name="action">Use LogWriter to write in the Console and the file at the same time</param>
    /// </summary>
    private static void ProgressForeachParallel<T>(this IEnumerable<T> collection,
        Func<T, string> elementID,
        LogWriter writer,
        ParallelOptions paralelOptions,
        bool transactional,
        bool showProgress,
        bool stopOnException,
        Action<T> action
    )
    {
        try
        {
            var col = collection.ToProgressEnumerator(out IProgressInfo pi);

            if (!Console.IsOutputRedirected && showProgress)
                lock (SafeConsole.SyncKey)
                    SafeConsole.WriteSameLine(pi.ToString());

            Exception? stopException = null;

            using (ExecutionContext.SuppressFlow())
                Parallel.ForEach(col,
                    paralelOptions ?? new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    (item, state) =>
                    {
                        using (HeavyProfiler.Log("ProgressForeach", () => elementID(item)))
                            try
                            {
                                if (transactional)
                                {
                                    using (var tr = Transaction.ForceNew())
                                    {
                                        action(item);
                                        tr.Commit();
                                    }
                                }
                                else
                                {
                                    action(item);
                                }
                            }
                            catch (Exception e)
                            {
                                writer(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                                writer(ConsoleColor.DarkRed, e.StackTrace!.Indent(4));

                                if (stopOnException || StopOnException != null && StopOnException(elementID(item), e))
                                    stopException = e;
                            }

                        if (!Console.IsOutputRedirected && showProgress)
                            lock (SafeConsole.SyncKey)
                                SafeConsole.WriteSameLine(pi.ToString());

                        if (stopException != null)
                            state.Break();

                    });

            if (stopException != null)
                throw stopException;
        }
        finally
        {
            if (!Console.IsOutputRedirected && showProgress)
                SafeConsole.ClearSameLine();
        }
    }



    public static string DefaultLogFolder = "Log";

    public static StreamWriter? TryOpenAutoFlush(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (!Path.IsPathRooted(fileName))
        {
            if (!Directory.Exists(DefaultLogFolder))
                Directory.CreateDirectory(DefaultLogFolder);

            fileName = Path.Combine(DefaultLogFolder, fileName);
        }

        var result = new StreamWriter(fileName, append: true)
        {
            AutoFlush = true
        };
        return result;
    }

    public static LogWriter GetFileWriter(StreamWriter logStreamWriter)
    {
        return (color, str, parameters) =>
        {
            string f = parameters.IsNullOrEmpty() ? str : str.FormatWith(parameters);
            lock (logStreamWriter)
                logStreamWriter.WriteLine(f);

        };
    }

    public static LogWriter GetConsoleWriter()
    {
        return (color, str, parameters) =>
        {
            lock (SafeConsole.SyncKey)
            {
                if (!Console.IsOutputRedirected)
                    SafeConsole.ClearSameLine();

                if (parameters.IsNullOrEmpty())
                    SafeConsole.WriteLineColor(color, str);
                else
                    SafeConsole.WriteLineColor(color, str, parameters);
            }
        };
    }


    public delegate void LogWriter(ConsoleColor color, string text, params object[] parameters);
}
