using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine.Properties;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Diagnostics;

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
        public class SqlPair
        {
            public readonly SqlPreCommand New;
            public readonly SqlPreCommand Remainig;

            public SqlPair(SqlPreCommand @new, SqlPreCommand remaining)
            {
                this.New = @new;
                this.Remainig = remaining;
            }
        }

        public abstract IEnumerable<SqlPreCommandSimple> Leaves();

        protected internal abstract void GenerateScript(StringBuilder sb);

        protected internal abstract void GenerateParameters(List<SqlParameter> list);

        protected internal abstract int NumParameters { get; }

        public abstract SqlPreCommandSimple ToSimple();

        public override string ToString()
        {
            return this.PlainSql();
        }

        protected internal abstract SqlPair Split(ref int remainingParameters);

        public IEnumerable<SqlPreCommand> Splits(int numParams)
        {
            SqlPreCommand rem = this;
            while (rem != null)
            {
                int remainingParams = numParams;
                SqlPreCommand.SqlPair pair = rem.Split(ref remainingParams);
                if (pair.New == null)
                    throw new InvalidOperationException("There is a SqlPreComandSimple with more than {0} parameters".Formato(numParams));

                yield return pair.New;

                rem = pair.Remainig;
            }
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

        static readonly Regex regex = new Regex(@"@[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*");
        /// <summary>
        /// For debugging purposes
        /// </summary>
        public static string PlainSql(this SqlPreCommand command)
        {
            SqlPreCommandSimple cs = command.ToSimple();
            if (cs.Parameters == null)
                return cs.Sql;

            var dic = cs.Parameters.ToDictionary(a=>a.ParameterName, a=>Encode(a.Value)); 

            return regex.Replace(cs.Sql, m=> dic.TryGetC(m.Value) ?? m.Value);
        }


        public static void OpenSqlFile(this SqlPreCommand command)
        {
            OpenSqlFile(command, "Sync {0:dd-MM-yyyy}.sql".Formato(DateTime.Now));
        }

        public static void OpenSqlFile(this SqlPreCommand command, string fileName)
        {
            string content = command.PlainSql(); 

            File.WriteAllText(fileName, content);

            Thread.Sleep(1000);

            Process.Start(fileName); 
        }

        static string Encode(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string)
                return "\'" + ((string)value).Replace("'", "''") + "'";

            if (value is DateTime)
                return "convert(datetime, '{0:s}', 126)".Formato(value);

            if (value is bool)
               return (((bool)value) ? 1 : 0).ToString();

            return value.ToString();
        }
    }

    public class SqlPreCommandSimple : SqlPreCommand
    {
        public string Sql { get; private set; }
        public List<SqlParameter> Parameters { get; private set; }
        internal IdentifiableEntity EntityToUpdate { get;  set; }

        public SqlPreCommandSimple(string sql)
        {
            this.Sql = sql;
        }

        public SqlPreCommandSimple(string sql, List<SqlParameter> parameters)
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

        protected internal override void GenerateParameters(List<SqlParameter> list)
        {
            if (Parameters != null)
                list.AddRange(Parameters);
        }

        public override SqlPreCommandSimple ToSimple()
        {
            return this;
        }

        protected internal override int NumParameters
        {
            get { return Parameters.TryCS(p => p.Count) ?? 0; }
        }

        protected internal override SqlPair Split(ref int remainingParameters)
        {
            return (remainingParameters -= this.NumParameters) >= 0 ?
                 new SqlPair(this, null) :
                 new SqlPair(null, this);
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
            bool borrar = false;
            foreach (SqlPreCommand com in Commands)
            {
                com.GenerateScript(sb);
                sb.Append(sep);
                borrar = true;
            }

            if (borrar) sb.Remove(sb.Length - sep.Length, sep.Length);
        }

        protected internal override void GenerateParameters(List<SqlParameter> list)
        {
            foreach (SqlPreCommand com in Commands)
                com.GenerateParameters(list);
        }

        public override SqlPreCommandSimple ToSimple()
        {
            StringBuilder sb = new StringBuilder();
            GenerateScript(sb);

            List<SqlParameter> parameters = new List<SqlParameter>();
            GenerateParameters(parameters);

            return new SqlPreCommandSimple(sb.ToString(), parameters);
        }

        static Dictionary<Spacing, string> separators = new Dictionary<Spacing, string>()
        {
            {Spacing.Simple, ";\r\n"},
            {Spacing.Double, ";\r\n\r\n"},
            {Spacing.Triple, ";\r\n\r\n\r\n"},
        };

        protected internal override int NumParameters
        {
            get { return Commands.Sum(c => c.NumParameters); }
        }

        protected internal override SqlPair Split(ref int remParameters)
        {
            SqlPair lastPair = null;
            int i = 0;
            for (; i < Commands.Length && (lastPair == null || lastPair.Remainig == null); i++)
                lastPair = Commands[i].Split(ref remParameters);

            //i es la posicion del siguiente a procesar
            if (i == Commands.Length && lastPair.Remainig == null)
                return new SqlPair(this, null);

            if (i == 0 || i == 1 && lastPair.New == null)
                return new SqlPair(null, this);

            return new SqlPair(
               Commands.Take(i - 1).And(lastPair.New).Combine(Spacing),
               Commands.Skip(i).PreAnd(lastPair.Remainig).Combine(Spacing));
        }

    }

}
