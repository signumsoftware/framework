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
        protected internal abstract bool EndsWithGo { get; }

        public abstract IEnumerable<SqlPreCommandSimple> Leaves();

        protected internal abstract void GenerateScript(StringBuilder sb);

        protected internal abstract void GenerateParameters(List<DbParameter> list);

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

        public List<SqlPreCommandSimple> PlainSqlSplitGOs()
        {
            return this.PlainSql().Split(new[] { "GO\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new SqlPreCommandSimple(s))
                .ToList();
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

        public abstract SqlPreCommand Clone(); 
    }

    public static class SqlPreCommandExtensions
    {
        public static SqlPreCommand Combine(this IEnumerable<SqlPreCommand> preCommands, Spacing spacing)
        {
            return SqlPreCommand.Combine(spacing, preCommands.ToArray());
        }

        public static SqlPreCommandSimple ToSimple(this SqlPreCommand command)
        {
            if (command == null)
                return null;

            if (command is SqlPreCommandSimple)
                return (SqlPreCommandSimple)command;


            var c = (SqlPreCommandConcat)command;
            StringBuilder sb = new StringBuilder();
            c.GenerateScript(sb);

            List<DbParameter> parameters = new List<DbParameter>();
            c.GenerateParameters(parameters);

            return new SqlPreCommandSimple(sb.ToString(), parameters);
        }
     
        public static void OpenSqlFileRetry(this SqlPreCommand command)
        {
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, "There are changes!");
            string file = command.OpenSqlFile();
            if (SafeConsole.Ask("Open again?"))
                Process.Start(file);
        }

        public static string OpenSqlFile(this SqlPreCommand command)
        {
            return OpenSqlFile(command, "Sync {0:dd-MM-yyyy hh_mm_ss}.sql".Formato(DateTime.Now));
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

            File.WriteAllText(fileName, content, Encoding.GetEncoding(1252));
        }
    }

    public class SqlPreCommandSimple : SqlPreCommand
    {
        protected internal override bool EndsWithGo
        {
            get { return GoAfter; }
        }

        public bool GoBefore { get; set; }
        public bool GoAfter { get; set; }

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

        public override IEnumerable<SqlPreCommandSimple> Leaves()
        {
            yield return this;
        }

        protected internal override void GenerateScript(StringBuilder sb)
        {
            sb.Append(Sql);
        }

        protected internal override void GenerateParameters(List<DbParameter> list)
        {
            if (Parameters != null)
                list.AddRange(Parameters);
        }

        protected internal override int NumParameters
        {
            get { return Parameters.Try(p => p.Count) ?? 0; }
        }

        static readonly Regex regex = new Regex(@"@[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*");
      
        internal static string Encode(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string)
                return "\'" + ((string)value).Replace("'", "''") + "'";

            if (value is Guid)
                return "\'" + ((Guid)value).ToString() + "'";

            if (value is DateTime)
                return "convert(datetime, '{0:s}', 126)".Formato(value);

            if (value is TimeSpan)
                return "convert(time, '{0:g}')".Formato(value);

            if (value is bool)
                return (((bool)value) ? 1 : 0).ToString();

            if (value is SqlHierarchyId)
                return "CAST('{0}' AS hierarchyid)".Formato(value);

            if (value.GetType().IsEnum)
                return Convert.ToInt32(value).ToString();

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

        public override SqlPreCommand Clone()
        {
            return new SqlPreCommandSimple(Sql, Parameters == null ? null : Parameters
                .Select(p => Connector.Current.CloneParameter(p))
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
    }

    public class SqlPreCommandConcat : SqlPreCommand
    {
        public Spacing Spacing { get; private set; }
        public SqlPreCommand[] Commands { get; private set; }

        internal SqlPreCommandConcat(Spacing spacing, SqlPreCommand[] commands)
        {
            this.Spacing = spacing;
            this.Commands = commands;
        }

        public override IEnumerable<SqlPreCommandSimple> Leaves()
        {
            return Commands.SelectMany(c => c.Leaves());
        }

        protected internal override void GenerateScript(StringBuilder sb)
        {
            string sep = separators[Spacing];
            for (int i = 0; i < Commands.Length; i++)
            {
                var cmd = Commands[i];
               
                cmd.GenerateScript(sb);
            
                if (i != Commands.Length - 1)
                {
                    if (!cmd.EndsWithGo)
                        sb.Append(";");

                    sb.Append(sep);
                }
            }
        }

        protected internal override void GenerateParameters(List<DbParameter> list)
        {
            foreach (SqlPreCommand com in Commands)
                com.GenerateParameters(list);
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

        protected internal override bool EndsWithGo
        {
            get { return Commands.Any() && Commands.Last().EndsWithGo; }
        }
    }

}
