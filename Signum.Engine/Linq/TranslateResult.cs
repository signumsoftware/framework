using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;
using Signum.Utilities;
using System.Collections;
using Signum.Engine.DynamicQuery;
using System.Data.SqlTypes;
using Signum.Entities;
using System.Data.Common;

namespace Signum.Engine.Linq
{
    public interface ITranslateResult
    {
        string CleanCommandText();

        SqlPreCommandSimple GetMainPreCommand();

        object Execute();

        LambdaExpression GetMainProjector(); 
    }

    interface IChildProjection
    {
        LookupToken Token { get; }

        void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever);

        SqlPreCommandSimple PreCommand();

        bool IsLazy { get; }
    }

    
    class EagerChildProjection<K, V>: IChildProjection
    {
        public LookupToken Token { get; set; }

        public string CommandText;
        internal List<DbParameter> Parameters;
        internal Expression<Func<IProjectionRow, KeyValuePair<K, V>>> ProjectorExpression;

        public void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever)
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, Parameters);
            using (HeavyProfiler.Log("SQL", () => command.Sql))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, V>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, V>>(reader, ProjectorExpression, lookups, retriever);

                IEnumerable<KeyValuePair<K, V>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, V>>(enumerator);

                try
                {
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);

                    lookups.Add(Token, lookUp);
                }
                catch (Exception ex)
                {
                    FieldReaderException fieldEx = enumerator.Reader.CreateFieldReaderException(ex);
                    fieldEx.Command = command;
                    fieldEx.Row = enumerator.Row;
                    fieldEx.Projector = ProjectorExpression;
                    throw fieldEx;
                }
            }
        }

        public SqlPreCommandSimple PreCommand()
        {
            return new SqlPreCommandSimple(CommandText, Parameters);
        }

        public bool IsLazy
        {
            get { return false; }
        }
    }


    class LazyChildProjection<K, V> : IChildProjection
    {
        public LookupToken Token { get; set; }

        public string CommandText;
        internal List<DbParameter> Parameters;
        internal Expression<Func<IProjectionRow, KeyValuePair<K, V>>> ProjectorExpression;

        public void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever)
        {
            Dictionary<K, MList<V>> requests = (Dictionary<K, MList<V>>)lookups.TryGetC(Token);

            if (requests == null)
                return;

            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, Parameters);
            using (HeavyProfiler.Log("SQL", () => command.Sql))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, V>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, V>>(reader, ProjectorExpression, lookups, retriever);

                IEnumerable<KeyValuePair<K, V>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, V>>(enumerator);

                try
                {
                    var ms = retriever.ModifiedState;
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);
                    foreach (var kvp in requests)
                    {
                        var results = lookUp[kvp.Key];

                        kvp.Value.AddRange(results);
                        kvp.Value.PostRetrieving();
                        kvp.Value.Modified = ms;
                    }
                }
                catch (Exception ex)
                {
                    FieldReaderException fieldEx = enumerator.Reader.CreateFieldReaderException(ex);
                    fieldEx.Command = command;
                    fieldEx.Row = enumerator.Row;
                    fieldEx.Projector = ProjectorExpression;
                    throw fieldEx;

                }
            }
        }

        public SqlPreCommandSimple PreCommand()
        {
            return new SqlPreCommandSimple(CommandText, Parameters);
        }

        public bool IsLazy
        {
            get { return true; }
        }
    }
    
    class TranslateResult<T> : ITranslateResult
    {
        public UniqueFunction? Unique { get; set; }

        internal List<IChildProjection> EagerProjections { get; set; }
        internal List<IChildProjection> LazyChildProjections { get; set; }

        Dictionary<LookupToken, IEnumerable> lookups;

        public string CommandText;
        internal List<DbParameter> Parameters;
        internal Expression<Func<IProjectionRow, T>> ProjectorExpression;

        public object Execute()
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                object result;
                using (IRetriever retriever = EntityCache.NewRetriever())
                {
                    if (EagerProjections.Any() || LazyChildProjections.Any())
                        lookups = new Dictionary<LookupToken, IEnumerable>();

                    foreach (var chils in EagerProjections)
                        chils.Fill(lookups, retriever);

                    SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, Parameters);

                    using (HeavyProfiler.Log("SQL", () => command.PlainSql()))
                    using (DbDataReader reader = Executor.UnsafeExecuteDataReader(command))
                    {
                        ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(reader, ProjectorExpression, lookups, retriever);

                        IEnumerable<T> enumerable = new ProjectionRowEnumerable<T>(enumerator);

                        try
                        {
                            if (Unique == null)
                                result = enumerable.ToList();
                            else
                                result = UniqueMethod(enumerable, Unique.Value);
                        }
                        catch (Exception ex)
                        {
                            FieldReaderException fieldEx = enumerator.Reader.CreateFieldReaderException(ex);
                            fieldEx.Command = command;
                            fieldEx.Row = enumerator.Row;
                            fieldEx.Projector = ProjectorExpression;
                            throw fieldEx;
                        }
                    }

                    foreach (var chils in LazyChildProjections)
                        chils.Fill(lookups, retriever);

                }
            
                return tr.Commit(result);
            }
        }

        internal T UniqueMethod(IEnumerable<T> enumerable, UniqueFunction uniqueFunction)
        {
            switch (uniqueFunction)
            {
                case UniqueFunction.First:  return enumerable.FirstEx();
                case UniqueFunction.FirstOrDefault: return enumerable.FirstOrDefault();
                case UniqueFunction.Single: return enumerable.SingleEx();
                case UniqueFunction.SingleOrDefault: return enumerable.SingleOrDefaultEx();
                default:
                    throw new InvalidOperationException();
            }
        }

        public string CleanCommandText()
        {
            try
            {
                SqlPreCommand eager = EagerProjections == null ? null : EagerProjections.Select(cp => cp.PreCommand()).Combine(Spacing.Double);

                SqlPreCommand main = GetMainPreCommand();

                SqlPreCommand lazy = LazyChildProjections  == null ? null : LazyChildProjections.Select(cp => cp.PreCommand()).Combine(Spacing.Double);

                return SqlPreCommandConcat.Combine(Spacing.Double,
                    eager == null ? null : new SqlPreCommandSimple("--------- Eager Client Joins ----------------"),
                    eager,
                    eager == null && lazy == null ? null : new SqlPreCommandSimple("--------- MAIN QUERY ------------------------"),
                    main,
                    lazy == null ? null :  new SqlPreCommandSimple("--------- Lazy Client Joins (if needed) -----"),
                    lazy).PlainSql(); 

            }
            catch
            {
                return CommandText;
            }
        }

        public SqlPreCommandSimple GetMainPreCommand()
        {
            return new SqlPreCommandSimple(CommandText, Parameters);
        }


        public LambdaExpression GetMainProjector()
        {
            return this.ProjectorExpression;
        }
    }

    class CommandResult
    {
        public string CommandText;
        public List<DbParameter> Parameters;

        public int ExecuteScalar()
        {
            SqlPreCommandSimple command = ToPreCommand();

            return (int)Executor.ExecuteScalar(command);
        }

        public SqlPreCommandSimple ToPreCommand()
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, Parameters);
            return command;
        }
    }
}
