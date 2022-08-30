using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Data.Common;
using System.Globalization;
using Signum.Engine.Maps;
using Npgsql;
using Microsoft.Data.SqlClient;

namespace Signum.Engine;

public enum Spacing
{
    Simple,
    Double,
    Triple
}

public abstract class SqlPreCommand
{
    public abstract IEnumerable<SqlPreCommandSimple> Leaves();

    public abstract SqlPreCommand Clone();

    public abstract bool GoBefore { get; set; }
    public abstract bool GoAfter { get; set; }

    protected internal abstract int NumParameters { get; }

    /// <summary>
    /// For debugging purposes
    /// </summary>
    public string PlainSql()
    {
        StringBuilder sb = new StringBuilder();
        this.PlainSql(sb);
        return sb.ToString();
    }




    protected internal abstract void PlainSql(StringBuilder sb);

    public override string ToString()
    {
        return this.PlainSql();
    }

    public static SqlPreCommand? Combine(Spacing spacing, params SqlPreCommand?[] sentences)
    {
        if (sentences.Contains(null))
            sentences = sentences.NotNull().ToArray();

        if (sentences.Length == 0)
            return null;

        if (sentences.Length == 1)
            return sentences[0];

        return new SqlPreCommandConcat(spacing, sentences as SqlPreCommand[]);
    }

    public abstract SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator);
}

public static class SqlPreCommandExtensions
{
    public static SqlPreCommand? Combine(this IEnumerable<SqlPreCommand?> preCommands, Spacing spacing)
    {
        return SqlPreCommand.Combine(spacing, preCommands.ToArray());
    }

    public static SqlPreCommand PlainSqlCommand(this SqlPreCommand command)
    {
        return command.PlainSql().SplitNoEmpty("GO\r\n")
            .Select(s => new SqlPreCommandSimple(s))
            .Combine(Spacing.Simple)!;
    }

    public static void OpenSqlFileRetry(this SqlPreCommand command)
    {
        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "There are changes!");
        var fileName = "Sync {0:dd-MM-yyyy HH_mm_ss}.sql".FormatWith(DateTime.Now);

        Save(command, fileName);
        SafeConsole.WriteLineColor(ConsoleColor.DarkYellow, command.PlainSql());

        Console.WriteLine("Script saved in:  " + Path.Combine(Directory.GetCurrentDirectory(), fileName));
        Console.WriteLine("Check the synchronization script before running it!");
        var answer = SafeConsole.AskRetry("Open or run?", "run", "open", "exit");

        if (answer == "open")
        {
            Thread.Sleep(1000);
            Open(fileName);
            if (SafeConsole.Ask("run now?"))
                ExecuteRetry(fileName);
        }
        else if (answer == "run")
        {
            ExecuteRetry(fileName);
        }
    }

    static void ExecuteRetry(string fileName)
    {
    retry:
        try
        {
            var script = File.ReadAllText(fileName);
            using (var tr = Transaction.ForceNew(System.Data.IsolationLevel.Unspecified))
            {
                ExecuteScript("script", script);
                tr.Commit();
            }
        }
        catch (ExecuteSqlScriptException)
        {
            Console.WriteLine("The current script is in saved in:  " + Path.Combine(Directory.GetCurrentDirectory(), fileName));
            var answer = SafeConsole.AskRetry("Open or retry?", "retry", "open", "exit");
            if (answer == "retry")
                goto retry;
            if (answer == "open")
            {
                Thread.Sleep(1000);
                Open(fileName);
                if (SafeConsole.Ask("run now?"))
                    ExecuteRetry(fileName);
            }
        }
    }

    public static int Timeout = 20 * 60;

    public static void ExecuteScript(string title, string script)
    {
        using (Connector.CommandTimeoutScope(Timeout))
        {
            var regex = new Regex(@"^ *(GO|USE \w+|USE \[[^\]]+\]) *(\r?\n|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var parts = regex.Split(script);

            var realParts = parts.Where(a => !string.IsNullOrWhiteSpace(a) && !regex.IsMatch(a)).ToArray();

            int pos = 0;
            for (pos = 0; pos < realParts.Length; pos++)
            {
                var currentPart = realParts[pos];

                try
                {

                    SafeConsole.WaitExecute("Executing {0} [{1}/{2}]".FormatWith(title, pos + 1, realParts.Length),
                        () => Executor.ExecuteNonQuery(currentPart));

                }
                catch (Exception ex)
                {
                    var sqlE = ex as SqlException ?? ex.InnerException as SqlException;
                    var pgE = ex as PostgresException ?? ex.InnerException as PostgresException;
                    if (sqlE == null && pgE == null)
                        throw;

                    Console.WriteLine();
                    Console.WriteLine();

                    var list = currentPart.Lines();

                    var lineNumer = (pgE?.Line?.ToInt() ?? sqlE!.LineNumber);

                    SafeConsole.WriteLineColor(ConsoleColor.Red, "ERROR:");

                    var min = Math.Max(0, lineNumer - 20);
                    var max = Math.Min(list.Length - 1, lineNumer + 20);

                    if (min > 0)
                        Console.WriteLine("...");

                    for (int i = min; i <= max; i++)
                    {
                        Console.Write(i + ": ");
                        SafeConsole.WriteLineColor(i == (lineNumer - 1) ? ConsoleColor.Red : ConsoleColor.DarkRed, list[i]);
                    }

                    if (max < list.Length - 1)
                        Console.WriteLine("...");

                    Console.WriteLine();

                    ex.Follow(a => a.InnerException).ToList().ForEach(e =>
                    {
                        var sql = e as SqlException;
                        var pg = e as PostgresException;

                        SafeConsole.WriteLineColor(ConsoleColor.DarkRed, (e == ex ? "" : "InnerException: ") + e.GetType().Name + " (Number {0}): ".FormatWith(pg?.SqlState ?? sql?.Number.ToString()));
                        SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);
                        Console.WriteLine();
                        Console.WriteLine();
                    });

                    Console.WriteLine();
                    throw new ExecuteSqlScriptException(ex.Message, ex);
                }
            }
        }
    }

    private static void Open(string fileName)
    {
        new Process
        {
            StartInfo = new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), fileName))
            {
                UseShellExecute = true
            }
        }.Start();
    }


    public static void Save(this SqlPreCommand command, string fileName)
    {
        string content = command.PlainSql();

        File.WriteAllText(fileName, content, Encoding.Unicode);
    }
}


public class ExecuteSqlScriptException : Exception
{
    public ExecuteSqlScriptException() { }
    public ExecuteSqlScriptException(string message) : base(message) { }
    public ExecuteSqlScriptException(string message, Exception inner) : base(message, inner) { }
    protected ExecuteSqlScriptException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public class SqlPreCommandSimple : SqlPreCommand
{
    public override bool GoBefore { get; set; }
    public override bool GoAfter { get; set; }

    public string Sql { get; private set; }
    public List<DbParameter>? Parameters { get; private set; }

    public SqlPreCommandSimple(string sql)
    {
        this.Sql = sql;
    }

    public SqlPreCommandSimple(string sql, List<DbParameter>? parameters)
    {
        this.Sql = sql;
        this.Parameters = parameters;
    }

    public void AlterSql(string sql)
    {
        this.Sql = sql;
    }

    public override IEnumerable<SqlPreCommandSimple> Leaves()
    {
        yield return this;
    }

    protected internal override int NumParameters
    {
        get { return Parameters?.Count ?? 0; }
    }

    static readonly Regex regex = new Regex(@"@[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*");

    internal static string Encode(object? value, bool simple = false)
    {
        if (value == null || value == DBNull.Value)
            return "NULL";

        if (value is string s)
            return "\'" + s.Replace("'", "''") + "'";

        if (value is char c)
            return "\'" + c.ToString().Replace("'", "''") + "'";

        if (value is Guid g)
            return "\'" + g.ToString() + "'";

        if (value is DateTime dt)
        {
            var str = dt.ToString("yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture);

            return Schema.Current.Settings.IsPostgres || simple ?
                "'{0}'".FormatWith(str) :
                "convert(datetime, '{0}', 126)".FormatWith(dt.ToString("yyyy-MM-ddThh:mm:ss.fff", CultureInfo.InvariantCulture));
        }

        if (value is TimeSpan ts)
        {
            var str = ts.ToString("g", CultureInfo.InvariantCulture);

            return Schema.Current.Settings.IsPostgres || simple ?
               "'{0}'".FormatWith(str) :
                "convert(time, '{0}')".FormatWith(str);
        }

        if (value is bool b)
        {
            if (Schema.Current.Settings.IsPostgres)
                return b.ToString();

            return (b ? 1 : 0).ToString();
        }

        if (Schema.Current.Settings.UdtSqlName.TryGetValue(value.GetType(), out var name))
            return "CAST('{0}' AS {1})".FormatWith(value, name);

        if (value.GetType().IsEnum)
            return Convert.ToInt32(value).ToString();

        if (value is byte[] bytes)
            return "0x" + BitConverter.ToString(bytes).Replace("-", "");

        if (value is IFormattable f)
            return f.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString()!;
    }

    protected internal override void PlainSql(StringBuilder sb)
    {
        if (Parameters.IsNullOrEmpty())
            sb.Append(Sql);
        else
        {
            var dic = Parameters.ToDictionary(a => a.ParameterName, a => Encode(a.Value));

            sb.Append(regex.Replace(Sql, m => dic.TryGetC(m.Value) ?? m.Value));
        }
    }

    public string sp_executesql()
    {
        var pars = this.Parameters.EmptyIfNull();
        var sqlBuilder = Connector.Current.SqlBuilder;

        var parameterVars = pars.ToString(p => "{0} {1}{2}".FormatWith(
            p.ParameterName,
            p is SqlParameter sp ? sp.SqlDbType.ToString() : ((NpgsqlParameter)p).NpgsqlDbType.ToString(),
            sqlBuilder.GetSizePrecisionScale(p.Size.DefaultToNull(), p.Precision.DefaultToNull(), p.Scale.DefaultToNull(), p.DbType == System.Data.DbType.Decimal)), ", ");
        var parameterValues = pars.ToString(p => p.ParameterName + " = " + Encode(p.Value, simple: true), ",\r\n");

        return @$"EXEC sp_executesql N'{this.Sql.Replace("'", "''")}', 
@params = N'{parameterVars}', 
{parameterValues}";
    }

    public override SqlPreCommand Clone()
    {
        return new SqlPreCommandSimple(Sql, Parameters?.Select(p => Connector.Current.CloneParameter(p)).ToList());
    }

    public SqlPreCommandSimple AddComment(string? comment)
    {
        if (comment.HasText())
        {
            int index = Sql.IndexOf("\r\n");
            if (index == -1)
                Sql = Sql + " -- " + comment;
            else
                Sql = Sql.Insert(index, " -- " + comment);
        }

        return this;
    }

    internal SqlPreCommandSimple ReplaceFirstParameter(string? variableName)
    {
        if (variableName == null)
            return this;

        this.ReplaceParameter(this.Parameters!.FirstEx(), variableName);
        return this;
    }

    internal SqlPreCommandSimple ReplaceParameter(DbParameter param, string variableName)
    {
        Sql = Regex.Replace(Sql, $@"(?<toReplace>{param.ParameterName})(\b|$)", variableName); //HACK
        Parameters!.Remove(param);
        return this;
    }

    public override SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator) => new SqlPreCommandSimple(regex.Replace(this.Sql, matchEvaluator), this.Parameters?.Select(p => Connector.Current.CloneParameter(p)).ToList())
    {
        GoAfter = GoAfter,
        GoBefore = GoBefore,
    };
}

public class SqlPreCommandConcat : SqlPreCommand
{
    public Spacing Spacing { get; private set; }
    public SqlPreCommand[] Commands { get; private set; }

    public override bool GoBefore { get { return this.Commands.First().GoBefore; } set { this.Commands.First().GoBefore = true; } }
    public override bool GoAfter { get { return this.Commands.Last().GoAfter; } set { this.Commands.Last().GoAfter = true; } }

    internal SqlPreCommandConcat(Spacing spacing, SqlPreCommand[] commands)
    {
        this.Spacing = spacing;
        this.Commands = commands;
    }

    public override IEnumerable<SqlPreCommandSimple> Leaves()
    {
        return Commands.SelectMany(c => c.Leaves());
    }

    static Dictionary<Spacing, string> separators = new Dictionary<Spacing, string>()
        {
            {Spacing.Simple, "\r\n"},
            {Spacing.Double, "\r\n\r\n"},
            {Spacing.Triple, "\r\n\r\n\r\n"},
        };

    protected internal override int NumParameters
    {
        get { return Commands.Sum(c => c.NumParameters); }
    }

    protected internal override void PlainSql(StringBuilder sb)
    {
        string sep = separators[Spacing];
        bool borrar = false;
        foreach (SqlPreCommand com in Commands)
        {
            var simple = com as SqlPreCommandSimple;

            if (simple != null && simple.GoBefore)
                sb.Append("GO\r\n");

            com.PlainSql(sb);

            if (simple != null && simple.GoAfter)
                sb.Append("\r\nGO");


            sb.Append(sep);
            borrar = true;
        }

        if (borrar) sb.Remove(sb.Length - sep.Length, sep.Length);
    }

    public override SqlPreCommand Clone()
    {
        return new SqlPreCommandConcat(Spacing, Commands.Select(c => c.Clone()).ToArray());
    }

    public override SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator)
    {
        return new SqlPreCommandConcat(Spacing, Commands.Select(c => c.Replace(regex, matchEvaluator)).ToArray());
    }
}

