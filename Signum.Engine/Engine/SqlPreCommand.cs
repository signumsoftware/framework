using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Data.Common;
using Microsoft.SqlServer.Types;
using System.Globalization;

namespace Signum.Engine
{
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

        public static SqlPreCommand Combine(Spacing spacing, params SqlPreCommand[] sentences)
        {
            if (sentences.Contains(null))
                sentences = sentences.NotNull().ToArray();

            if (sentences.Length == 0)
                return null;

            if (sentences.Length == 1)
                return sentences[0];

            return new SqlPreCommandConcat(spacing, sentences);
        }


    }

    public static class SqlPreCommandExtensions
    {
        public static SqlPreCommand Combine(this IEnumerable<SqlPreCommand> preCommands, Spacing spacing)
        {
            return SqlPreCommand.Combine(spacing, preCommands.ToArray());
        }

        public static SqlPreCommand PlainSqlCommand(this SqlPreCommand command)
        {
            if (command == null)
                return null;

            return command.PlainSql().SplitNoEmpty("GO\r\n" )
                .Select(s => new SqlPreCommandSimple(s))
                .Combine(Spacing.Simple);
        }

        public static bool AvoidOpenOpenSqlFileRetry = true;
     
        public static void OpenSqlFileRetry(this SqlPreCommand command)
        {
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "There are changes!");
            string file = command.OpenSqlFile();
            if (!AvoidOpenOpenSqlFileRetry && SafeConsole.Ask("Open again?"))
                Process.Start(file);
        }

        public static string OpenSqlFile(this SqlPreCommand command)
        {
            return OpenSqlFile(command, "Sync {0:dd-MM-yyyy hh_mm_ss}.sql".FormatWith(DateTime.Now));
        }

        public static string OpenSqlFile(this SqlPreCommand command, string fileName)
        {
            Save(command, fileName);

            Thread.Sleep(1000);

            Process.Start(fileName);

            return fileName;
        }

        public static void Save(this SqlPreCommand command, string fileName)
        {
            string content = command.PlainSql();

            File.WriteAllText(fileName, content, Encoding.Unicode);
        }
    }

    public class SqlPreCommandSimple : SqlPreCommand
    {
        public override bool GoBefore { get; set; }
        public override bool GoAfter { get; set; }

        public string Sql { get; private set; }
        public List<DbParameter> Parameters { get; private set; }

        public SqlPreCommandSimple(string sql)
        {
            this.Sql = sql;
        }

        public SqlPreCommandSimple(string sql, List<DbParameter> parameters)
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
      
        internal static string Encode(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string s)
                return "\'" + s.Replace("'", "''") + "'";

            if (value is Guid g)
                return "\'" + g.ToString() + "'";

            if (value is DateTime dt)
                return "convert(datetime, '{0}', 126)".FormatWith(dt.ToString("yyyy-MM-ddThh:mm:ss.fff", CultureInfo.InvariantCulture));

            if (value is TimeSpan ts)
                return "convert(time, '{0:g}')".FormatWith(ts.ToString("g", CultureInfo.InvariantCulture));

            if (value is bool b)
                return (b ? 1 : 0).ToString();

            if (value is SqlHierarchyId sh)
                return "CAST('{0}' AS hierarchyid)".FormatWith(sh);

            if (value.GetType().IsEnum)
                return Convert.ToInt32(value).ToString();

            if (value is byte[] bytes)
                return "0x" + BitConverter.ToString(bytes).Replace("-", "");

            return value.ToString();
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

            var parameterVars = pars.ToString(p => $"{p.ParameterName} {((SqlParameter)p).SqlDbType.ToString()}{SqlBuilder.GetSizeScale(p.Size.DefaultToNull(), p.Scale.DefaultToNull())}", ", ");
            var parameterValues = pars.ToString(p => Encode(p.Value), ",");

            return $"EXEC sp_executesql N'{this.Sql}', N'{parameterVars}', {parameterValues}";
        }

        public override SqlPreCommand Clone()
        {
            return new SqlPreCommandSimple(Sql, Parameters?.Select(p => Connector.Current.CloneParameter(p))
                .ToList());
        }

        public SqlPreCommandSimple AddComment(string comment)
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

        public SqlPreCommandSimple ReplaceFirstParameter(string variableName)
        {
            if (variableName == null)
                return this;

            var first = Parameters.FirstEx();
            Sql = Regex.Replace(Sql, $@"(?<toReplace>{first.ParameterName})(\b|$)", variableName); //HACK
            Parameters.Remove(first);
            return this;
        }
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
    }

}
