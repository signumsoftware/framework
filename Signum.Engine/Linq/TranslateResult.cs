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

        SqlPreCommandSimple MainCommand { get; set; }

        object Execute();

        LambdaExpression GetMainProjector(); 
    }

    interface IChildProjection
    {
        LookupToken Token { get; set; }

        void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever);

        SqlPreCommandSimple Command { get; set; }

        bool IsLazy { get; }
    }

    
    class EagerChildProjection<K, V>: IChildProjection
    {
        public LookupToken Token { get; set; }

        public SqlPreCommandSimple Command { get; set; }
        internal Expression<Func<IProjectionRow, KeyValuePair<K, V>>> ProjectorExpression;

        public void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever)
        {
            using (HeavyProfiler.Log("SQL", () => Command.Sql))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(Command))
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
                    fieldEx.Command = Command;
                    fieldEx.Row = enumerator.Row;
                    fieldEx.Projector = ProjectorExpression;
                    throw fieldEx;
                }
            }
        }

        public bool IsLazy
        {
            get { return false; }
        }
    }


    class LazyChildProjection<K, V> : IChildProjection
    {
        public LookupToken Token { get; set; }

        public SqlPreCommandSimple Command { get; set; }
        internal Expression<Func<IProjectionRow, KeyValuePair<K, MList<V>.RowIdValue>>> ProjectorExpression;

        public void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever)
        {
            Dictionary<K, MList<V>> requests = (Dictionary<K, MList<V>>)lookups.TryGetC(Token);

            if (requests == null)
                return;

            using (HeavyProfiler.Log("SQL", () => Command.Sql))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(Command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdValue>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdValue>>(reader, ProjectorExpression, lookups, retriever);

                IEnumerable<KeyValuePair<K, MList<V>.RowIdValue>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, MList<V>.RowIdValue>>(enumerator);

                try
                {
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);
                    foreach (var kvp in requests)
                    {
                        var results = lookUp[kvp.Key];

                        ((IMListPrivate<V>)kvp.Value).InnerList.AddRange(results);
                        ((IMListPrivate<V>)kvp.Value).InnerListModified(results.Select(a => a.Value).ToList(), null);
                        retriever.ModifiablePostRetrieving(kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    FieldReaderException fieldEx = enumerator.Reader.CreateFieldReaderException(ex);
                    fieldEx.Command = Command;
                    fieldEx.Row = enumerator.Row;
                    fieldEx.Projector = ProjectorExpression;
                    throw fieldEx;

                }
            }
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

        public SqlPreCommandSimple MainCommand { get; set; }
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

                    using (HeavyProfiler.Log("SQL", () => MainCommand.PlainSql()))
                    using (DbDataReader reader = Executor.UnsafeExecuteDataReader(MainCommand))
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
                            fieldEx.Command = MainCommand;
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
                SqlPreCommand eager = EagerProjections == null ? null : EagerProjections.Select(cp => cp.Command).Combine(Spacing.Double);

                SqlPreCommand lazy = LazyChildProjections  == null ? null : LazyChildProjections.Select(cp => cp.Command).Combine(Spacing.Double);

                return SqlPreCommandConcat.Combine(Spacing.Double,
                    eager == null ? null : new SqlPreCommandSimple("--------- Eager Client Joins ----------------"),
                    eager,
                    eager == null && lazy == null ? null : new SqlPreCommandSimple("--------- MAIN QUERY ------------------------"),
                    MainCommand,
                    lazy == null ? null :  new SqlPreCommandSimple("--------- Lazy Client Joins (if needed) -----"),
                    lazy).PlainSql(); 

            }
            catch
            {
                return MainCommand.Sql;
            }
        }

        public LambdaExpression GetMainProjector()
        {
            return this.ProjectorExpression;
        }
    }
}
