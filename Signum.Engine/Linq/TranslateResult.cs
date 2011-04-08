using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Data;
using Signum.Utilities;
using System.Collections;
using Signum.Engine.DynamicQuery;

namespace Signum.Engine.Linq
{
    interface ITranslateResult
    {
        string CommandText { get; }
        UniqueFunction? Unique { get; }

        string CleanCommandText();

        object Execute();
    }

    interface IChildProjection
    {
        ProjectionToken Name { get; }

        void Fill(Dictionary<ProjectionToken, IEnumerable> lookups, Retriever retriever);

        SqlPreCommandSimple PreCommand();
    }



    class ChildProjection<K, V> : IChildProjection
    {
        public ProjectionToken Name { get; set; }

        public string CommandText { get; set; }
        internal Expression<Func<SqlParameter[]>> GetParametersExpression;
        internal Func<SqlParameter[]> GetParameters;
        internal Expression<Func<IProjectionRow, KeyValuePair<K, V>>> ProjectorExpression;

        public void Fill(Dictionary<ProjectionToken, IEnumerable> lookups, Retriever retriever)
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, GetParameters().ToList());
            using (HeavyProfiler.Log("SQL", command.Sql))
            using (SqlDataReader reader = Executor.UnsafeExecuteDataReader(command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, V>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, V>>(reader, ProjectorExpression, retriever, lookups);

                IEnumerable<KeyValuePair<K, V>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, V>>(enumerator);

                lookups.Add(Name, enumerabe.ToLookup(a => a.Key, a => a.Value));
            }
        }


        public SqlPreCommandSimple PreCommand()
        {
            return new SqlPreCommandSimple(CommandText, GetParameters().ToList());
        }
    };

    class TranslateResult<T> : ITranslateResult
    {
        public UniqueFunction? Unique { get; set; }

        internal List<IChildProjection> ChildProjections { get; set; }

        Dictionary<ProjectionToken, IEnumerable> lookups;

        public string CommandText { get; set; }
        internal Expression<Func<SqlParameter[]>> GetParametersExpression;
        internal Func<SqlParameter[]> GetParameters;
        internal Expression<Func<IProjectionRow, T>> ProjectorExpression;

        public object Execute()
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Retriever retriever = new Retriever() { InQuery = true };

                if (ChildProjections != null)
                {
                    lookups = new Dictionary<ProjectionToken, IEnumerable>();
                    foreach (var chils in ChildProjections)
                        chils.Fill(lookups, retriever);
                }

                SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, GetParameters().ToList());

                object result;
                using (HeavyProfiler.Log("SQL", command.Sql))
                using (SqlDataReader reader = Executor.UnsafeExecuteDataReader(command))                
                {
                    ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(reader, ProjectorExpression, retriever, lookups);

                    IEnumerable<T> enumerable = new ProjectionRowEnumerable<T>(enumerator);

                    if (Unique == null)
                        result = enumerable.ToList();
                    else
                        result = UniqueMethod(enumerable, Unique.Value);
                }

                retriever.ProcessAll();

                return tr.Commit(result);
            }
        }

        internal T UniqueMethod(IEnumerable<T> enumerable, UniqueFunction uniqueFunction)
        {
            switch (uniqueFunction)
            {
                case UniqueFunction.First:  return enumerable.First();
                case UniqueFunction.FirstOrDefault: return enumerable.FirstOrDefault();
                case UniqueFunction.Single: return enumerable.Single();
                case UniqueFunction.SingleOrDefault: return enumerable.SingleOrDefault();
                default:
                    throw new InvalidOperationException();
            }
        } 

        #region ITranslateResult Members


        public string CleanCommandText()
        {
            try
            {
                SqlPreCommand main = new SqlPreCommandSimple(CommandText, GetParameters().ToList());

                if (ChildProjections != null)
                    return ChildProjections.Select(c => c.PreCommand()).And(main).Combine(Spacing.Double).PlainSql();

                return main.PlainSql();
            }
            catch
            {
                return CommandText;
            }
        }

        #endregion

        public object DynamiQuery { get; set; }
    }

    class CommandResult
    {
        public string CommandText { get; set; }
        internal Expression<Func<SqlParameter[]>> GetParametersExpression;
        internal Func<SqlParameter[]> GetParameters;

        public int Execute()
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, GetParameters().ToList());

            return (int)Executor.ExecuteScalar(command);
        }
    }
}
