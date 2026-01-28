using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Data.Common;
using System.Globalization;
using Signum.Engine.Maps;
using Npgsql;
using Microsoft.Data.SqlClient;
using Signum.Utilities.ExpressionTrees;
using System.Diagnostics.Metrics;
using Npgsql.PostgresTypes;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

namespace Signum.Engine.Sync;

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

    public abstract bool HasNoTransaction { get; }
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

    public static SqlPreCommand TransactionBlock(this SqlPreCommand command, string name)
    {
        return SqlPreCommand.Combine(Spacing.Double,
            new SqlPreCommandSimple("GO--BEGIN " + name),
            command,
            new SqlPreCommandSimple("GO--END " + name)
            )!;
    }

    public static SqlPreCommand PlainSqlCommand(this SqlPreCommand command)
    {
        (SqlPreCommand? before, SqlPreCommand? after) noTransaction = default;
        if (command.HasNoTransaction)
        {
            if (command is SqlPreCommandSimple)
                return command;

            noTransaction = ((SqlPreCommandConcat)command).ExtractNoTransaction();
        }

        var list = command.PlainSql().SplitNoEmpty("GO\n")
            .Select(s => (SqlPreCommand)new SqlPreCommandSimple(s) { GoAfter = true })
            .ToList();

        if (noTransaction.before != null)
            list.Insert(0, noTransaction.before!);

        if (noTransaction.after != null)
            list.Add(noTransaction.after);

        return list.Combine(Spacing.Simple)!;
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
                ExecuteScript("script", script, autoRun: false); 
                tr.Commit();
            }
        }
        catch (ExecuteSqlScriptException e)
        {
            if (e.InnerException is SqlException sqle && sqle.Number == 574)
            {
                if (SafeConsole.Ask("Execute without transaction?"))
                {
                    var script = File.ReadAllText(fileName);
                    ExecuteScript("script", script, autoRun: false);
                    return;
                }
                else
                    throw;
            }

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

    public static int DefaultScriptTimeout = 20 * 60;

    public static Regex regexBeginEnd = new Regex(@"^ *(GO--(?<type>BEGIN|END)) *(?<key>.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    static Dictionary<string, string> ExtractBeginEndParts(ref string script)
    {
        var matches = regexBeginEnd.Matches(script);

        var result = new Dictionary<string, string>();
        
        if ((matches.Count % 2) != 0)
            throw new InvalidOperationException($"Un-even number of GO--BEGIN/END blocks ({matches.Count}):\n{matches.ToString(a => a.Value, "\n")}");

        StringBuilder sb = new StringBuilder();
        var lastIndex = 0;

        foreach (var pair in matches.Chunk(2).ToList())
        {
            var begin = pair[0];
            var end = pair[1];
            if (begin.Groups["type"].Value != "BEGIN")
                throw new InvalidOperationException($"Unexpected {begin.Groups["type"].Value} in: {begin}");

            if (end.Groups["type"].Value != "END")
                throw new InvalidOperationException($"Unexpected {end.Groups["type"].Value} in: {end}");

            var beginKey = begin.Groups["key"].Value.Trim();
            var endKey= begin.Groups["key"].Value.Trim();

            if (beginKey != endKey)
                throw new InvalidOperationException($"GO--END key does not match with GO--BEGIN:\n{begin}\n{end}");

            if(begin.Index != lastIndex)
            {
                sb.Append(script.Substring(lastIndex, begin.Index - lastIndex));
            }

            result.Add(beginKey, script.Substring(begin.EndIndex(), end.Index - begin.EndIndex()));

            lastIndex = end.EndIndex();
        }

        if(lastIndex != script.Length)
        {
            sb.Append(script.Substring(lastIndex));
        }

        script = sb.ToString();

        return result;
    }


    public static void ExecuteScript(string title, string script, bool autoRun)
    {
        using (Connector.CommandTimeoutScope(Connector.ScopeTimeout ?? DefaultScriptTimeout))
        {
            List<KeyValuePair<string, string>> beginEndParts = ExtractBeginEndParts(ref script).ToList();

            string[] realParts = SplitGOs(script);

            Schema.Current.ExecuteExecuteAs();

            if (Connector.Current is PostgreSqlConnector)
            {
                for (int pos = 0; pos < realParts.Length; pos++)
                {
                    var currentPart = realParts[pos];

                    var statements = PostgresStatementSplitter.SplitPostgresScript(currentPart);

                    for (int i = 0; i < statements.Count; i++)
                    {
                        var statement = statements[i];
                        try
                        {
                            SafeConsole.WaitExecute("Executing {0} [{1}/{2}]{3}".FormatWith(title, pos + 1, realParts.Length, statements.Count <= 1 ? "" : $" statement {i + 1}/{statements.Count}"),
                                () => Executor.ExecuteNonQuery(statement));
                        }
                        catch (Exception ex)
                        {
                            var pgE = ex as PostgresException ?? ex.InnerException as PostgresException;
                            if (pgE == null)
                                throw;

                            PrintExceptionLine(statement, ex, null, pgE);

                            Console.WriteLine();
                            throw new ExecuteSqlScriptException(ex.Message, ex);
                        }
                    }
                }
            }
            else
            {
                for (int pos = 0; pos < realParts.Length; pos++)
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
                        if (sqlE == null)
                            throw;

                        PrintExceptionLine(currentPart, ex, sqlE, null);

                        Console.WriteLine();
                        throw new ExecuteSqlScriptException(ex.Message, ex);
                    }
                }
            }



            bool allYes = false;
            for (int i = 0; i < beginEndParts.Count; i++)
            {
                using (var tr = Transaction.NamedSavePoint("SavePoint" + i))
                {
                    var kvp = beginEndParts[i];
                    try
                    {
                        SafeConsole.WaitExecute("Executing UserAssets [{1}/{2}]".FormatWith(title, i + 1, beginEndParts.Count),
                                () => Executor.ExecuteNonQuery(kvp.Value));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        SafeConsole.WriteLineColor(ConsoleColor.Red, "Error in UserAsset " + kvp.Key);
                        SafeConsole.WriteLineColor(ConsoleColor.DarkRed, ex.GetType().Name + ":" + ex.Message);

                        var sqlE = ex as SqlException ?? ex.InnerException as SqlException;
                        var pgE = ex as PostgresException ?? ex.InnerException as PostgresException;


                        var answer = autoRun || allYes ? "yes" : SafeConsole.Ask("Continue anyway?", new[] { "yes", "no", "all yes", sqlE != null || pgE != null ? "+ exception details" : null }.NotNull().ToArray());
                        if (answer == "+ exception details")
                        {
                            PrintExceptionLine(kvp.Value, ex, sqlE, pgE);
                            answer = SafeConsole.Ask("Continue anyway?", new[] { "yes", "no", "all yes" });
                        }

                        switch (answer)
                        {
                            case "no": throw new ExecuteSqlScriptException(ex.Message, ex);
                            case "yes": continue;
                            case "all yes":
                                {
                                    allYes = true;
                                    continue;
                                }
                        }
                    }

                    tr.Commit();
                }

            }
        }
    }

    static Regex regex = new Regex(@"^ *(GO|USE \w+|USE \[[^\]]+\]) *(\r?\n|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    static string[] SplitGOs(string script)
    {
        var parts = regex.Split(script);

        var realParts = parts.Where(a => !string.IsNullOrWhiteSpace(a) && !regex.IsMatch(a)).ToArray();
        return realParts;
    }

    private static void PrintExceptionLine(string currentPart, Exception ex, SqlException? sqlE, PostgresException? pgE)
    {
        Console.WriteLine();
        Console.WriteLine();

        var list = currentPart.Lines();

        var lineNumber = (sqlE?.LineNumber ?? currentPart.Substring(0, pgE!.Position).Lines().Count());

        SafeConsole.WriteLineColor(ConsoleColor.Red, "ERROR:");

        var min = Math.Max(0, lineNumber - 20);
        var max = Math.Min(list.Length - 1, lineNumber + 20);

        if (min > 0)
            Console.WriteLine("...");

        for (int i = min; i <= max; i++)
        {
            Console.Write(i + ": ");
            SafeConsole.WriteLineColor(i == (lineNumber - 1) ? ConsoleColor.DarkRed : ConsoleColor.DarkYellow, list[i]);
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
}

public enum NoTransactionMode
{
    BeforeScript,
    AfterScript,
}

public class SqlPreCommandSimple : SqlPreCommand
{
    public NoTransactionMode? NoTransaction { get; set; }
    public override bool HasNoTransaction => NoTransaction != null;
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

    internal static string LiteralValue(object? value, bool simple = false)
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
        {
            if(Schema.Current.Settings.IsPostgres)
                return "'\\x" + BitConverter.ToString(bytes).Replace("-", "") + "'";

            return "0x" + BitConverter.ToString(bytes).Replace("-", "");
        }

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
            var dic = Parameters.ToDictionary(a => a.ParameterName, a => LiteralValue(a.Value));

            sb.Append(regex.Replace(Sql, m => dic.TryGetC(m.Value) ?? m.Value));
        }
    }


    public string PreparedQuery()
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        if (!sqlBuilder.IsPostgres)
            return sp_executesql();

        var ticks = DateTime.UtcNow.Ticks;

        var pars = this.Parameters.EmptyIfNull();

        var parameter = pars.Select(p => new
        {
            Name = p.ParameterName,
            Value = LiteralValue(p.Value, simple: true),
            ParameterType = "{0}{1}".FormatWith(
                   p is SqlParameter sp ? sp.SqlDbType.ToString() : ((NpgsqlParameter)p).NpgsqlDbType.ToString(),
                   sqlBuilder.GetSizePrecisionScale(p.Size.DefaultToNull(), p.Precision.DefaultToNull(), p.Scale.DefaultToNull(), p.DbType == System.Data.DbType.Decimal)),
        }).ToList();

        var index = 1;
        var paramToIndex = parameter.ToDictionary(a => a.Name.StartsWith("@") ? a.Name : "@" + a.Name, a => index++);

        var pgsql = Regex.Replace(this.Sql, @"@(\w+)", m => $"${paramToIndex.GetOrThrow(m.Value)}");

        return $"""
            DEALLOCATE ALL;
            PREPARE my_query ({parameter.ToString(a => a.ParameterType, ", ")}) AS
            {pgsql};
            EXECUTE my_query ({parameter.ToString(a => a.Value + "::" + a.ParameterType, ", ")});
            """;
    }

    public string sp_executesql()
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        if (sqlBuilder.IsPostgres)
            return PreparedQuery();

        var pars = this.Parameters.EmptyIfNull();

        var parameterVars = pars.ToString(p => "{0} {1}{2}".FormatWith(
            p.ParameterName,
            p is SqlParameter sp ? sp.SqlDbType.ToString() : ((NpgsqlParameter)p).NpgsqlDbType.ToString(),
            sqlBuilder.GetSizePrecisionScale(p.Size.DefaultToNull(), p.Precision.DefaultToNull(), p.Scale.DefaultToNull(), p.DbType == System.Data.DbType.Decimal)), ", ");
       
        var parameterValues = pars.ToString(p => p.ParameterName + " = " + LiteralValue(p.Value, simple: true), ",\n");

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
            int index = Sql.IndexOf("\n");
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

    public override bool HasNoTransaction => this.Commands.Any(a => a.HasNoTransaction);
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
            {Spacing.Simple, "\n"},
            {Spacing.Double, "\n\n"},
            {Spacing.Triple, "\n\n\n"},
        };

    protected internal override int NumParameters
    {
        get { return Commands.Sum(c => c.NumParameters); }
    }

    protected internal override void PlainSql(StringBuilder sb)
    {
        string sep = separators[Spacing];
        bool remove = false;
        foreach (SqlPreCommand com in Commands)
        {
            var simple = com as SqlPreCommandSimple;

            if (simple != null && simple.GoBefore)
                sb.Append("GO\n");

            com.PlainSql(sb);

            if (simple != null && simple.GoAfter)
                sb.Append("\nGO");


            sb.Append(sep);
            remove = true;
        }

        if (remove)
            sb.Remove(sb.Length - sep.Length, sep.Length);
    }

    public override SqlPreCommand Clone()
    {
        return new SqlPreCommandConcat(Spacing, Commands.Select(c => c.Clone()).ToArray());
    }

    public override SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator)
    {
        return new SqlPreCommandConcat(Spacing, Commands.Select(c => c.Replace(regex, matchEvaluator)).ToArray());
    }

    public (SqlPreCommand? before, SqlPreCommand? after) ExtractNoTransaction()
    {
        if (!HasNoTransaction)
            return (null, null);

        var noTransaction = new List<(SqlPreCommand? before, SqlPreCommand? after)>();

        Commands = Commands.Select(a =>
        {
            if (a is SqlPreCommandConcat concat)
            {
                var nt = concat.ExtractNoTransaction();

                if (nt.before != null || nt.after != null)
                    noTransaction.Add(nt);

                if (concat.Commands.Count() == 0)
                    return null;

                if (concat.Commands.Length == 1)
                    return concat.Commands.SingleEx();

                return concat;
            }

            if (a is SqlPreCommandSimple simple)
            {
                if (simple.NoTransaction != null)
                {
                    if (simple.NoTransaction == NoTransactionMode.BeforeScript)
                        noTransaction.Add((simple, null));
                    else
                        noTransaction.Add((null, simple));

                    return null;
                }
                else
                    return simple;
            }

            return null;
        }).NotNull().ToArray();

        return (
            noTransaction.Select(a => a.before).Combine(this.Spacing),
            noTransaction.Select(a => a.after).Combine(this.Spacing)
        );
    }
}



public class SqlPreCommandPostgresDoBlock : SqlPreCommand
{
    public override bool HasNoTransaction => false;

    public override bool GoBefore { get; set; }
    public override bool GoAfter { get; set; }

    public SqlPreCommand[] Declarations { get; }
    public SqlPreCommand Body { get; }

    public SqlPreCommandPostgresDoBlock(SqlPreCommand[] declarations, SqlPreCommand body)
    {
        Declarations = declarations;
        Body = body;
    }

    protected internal override int NumParameters
    {
        get { return Declarations.Sum(a=>a.NumParameters) + Body.NumParameters; }
    }

    public override SqlPreCommand Clone()
    {
        return new SqlPreCommandPostgresDoBlock(
            Declarations.Select(c => c.Clone()).ToArray(),
            Body.Clone());
    }

    public SqlPreCommandSimple ToSqlPreCommandSimple()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("DO $$");
        sb.AppendLine("DECLARE");
        foreach (var decl in Declarations)
        {
            sb.AppendLine(decl.PlainSql().Indent(4));
        }
        sb.AppendLine("BEGIN");
        sb.AppendLine(Body.PlainSql().Indent(4));
        sb.AppendLine("END $$;");
        return new SqlPreCommandSimple(sb.ToString().Replace("\r\n", "\n"));
    }

    public override SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator)
    {
        return new SqlPreCommandPostgresDoBlock(
            Declarations.Select(c => c.Replace(regex, matchEvaluator)).ToArray(),
            Body.Replace(regex, matchEvaluator));
    }

    protected internal override void PlainSql(StringBuilder sb)
    {
        ToSqlPreCommandSimple().PlainSql(sb);
    }

    public override IEnumerable<SqlPreCommandSimple> Leaves()
    {
        return [this.ToSqlPreCommandSimple()];
    }

    public SqlPreCommandPostgresDoBlock SimplifyNested()
    {
        var newDeclarations = Declarations.ToList();

        var newBody = Simplify(Body);

        SqlPreCommand Simplify(SqlPreCommand exp)
        {
            if (exp is SqlPreCommandPostgresDoBlock pgDo)
            {
                newDeclarations.AddRange(pgDo.Declarations);
                return Simplify(pgDo.Body);
            }
            else if (exp is SqlPreCommandConcat concat)
            {
                var simplifiedCommands = concat.Commands.Select(Simplify).ToArray();
                if (simplifiedCommands.SequenceEqual(concat.Commands))
                    return exp;

                return new SqlPreCommandConcat(concat.Spacing, simplifiedCommands);
            }
            else if (exp is SqlPreCommandSimple simple)
                return exp;
            else 
                throw new UnexpectedValueException(exp);
        }

        if (newBody == Body)
            return this;

        return new SqlPreCommandPostgresDoBlock(newDeclarations.ToArray(), newBody);
    }
}


public class SqlPreCommand_WithHistory : SqlPreCommand
{
    public SqlPreCommand? Normal;
    public SqlPreCommand? History;

    public SqlPreCommand_WithHistory(SqlPreCommand? normal, SqlPreCommand? history)
    {
        Normal = normal;
        History = history;
    }

    public override bool HasNoTransaction => throw new NotImplementedException();

    public override bool GoBefore { get; set; }
    public override bool GoAfter { get; set; }

    protected internal override int NumParameters => throw new NotImplementedException();

    public override SqlPreCommand Clone() => throw new NotImplementedException();

    public override IEnumerable<SqlPreCommandSimple> Leaves() => throw new NotImplementedException();

    public override SqlPreCommand Replace(Regex regex, MatchEvaluator matchEvaluator)  => throw new NotImplementedException();

    protected internal override void PlainSql(StringBuilder sb) => throw new NotImplementedException();

    public static SqlPreCommand? ForNormal(SqlPreCommand? command)
    {
        if (command is null)
            return null;

        if (command is SqlPreCommandSimple s)
            return s;

        if (command is SqlPreCommandConcat c)
            return new SqlPreCommandConcat(c.Spacing, c.Commands.Select(ForNormal).NotNull().ToArray());

        if (command is SqlPreCommand_WithHistory h)
            return h.Normal;

        throw new UnexpectedValueException(command);
    }

    public static SqlPreCommand? ForHistory(SqlPreCommand? command)
    {
        if (command is null)
            return null;

        if (command is SqlPreCommandSimple s)
            return s;

        if (command is SqlPreCommandConcat c)
            return c.Commands.Select(ForHistory).Combine(c.Spacing);

        if (command is SqlPreCommand_WithHistory h)
            return h.History;

        throw new UnexpectedValueException(command);
    }
}
