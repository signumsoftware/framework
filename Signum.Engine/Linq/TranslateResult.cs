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
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Linq
{
    public interface ITranslateResult
    {
        string CleanCommandText();

        SqlPreCommandSimple MainCommand { get; set; }

        object Execute();
        Task<object> ExecuteAsync(CancellationToken token);

        LambdaExpression GetMainProjector(); 
    }

    interface IChildProjection
    {
        LookupToken Token { get; set; }

        void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever);
        Task FillAsync(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever, CancellationToken token);

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
            using (HeavyProfiler.Log("SQL", () => Command.sp_executesql()))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(Command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, V>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, V>>(reader, ProjectorExpression, lookups, retriever, CancellationToken.None);

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

        public async Task FillAsync(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever, CancellationToken token)
        {
            using (HeavyProfiler.Log("SQL", () => Command.sp_executesql()))
            using (DbDataReader reader = await Executor.UnsafeExecuteDataReaderAsync(Command, token: token))
            {
                ProjectionRowEnumerator<KeyValuePair<K, V>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, V>>(reader, ProjectorExpression, lookups, retriever, token);

                IEnumerable<KeyValuePair<K, V>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, V>>(enumerator);

                try
                {
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);

                    lookups.Add(Token, lookUp);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
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
        internal Expression<Func<IProjectionRow, KeyValuePair<K, MList<V>.RowIdElement>>> ProjectorExpression;

        public void Fill(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever)
        {
            Dictionary<K, MList<V>> requests = (Dictionary<K, MList<V>>)lookups.TryGetC(Token);

            if (requests == null)
                return;

            using (HeavyProfiler.Log("SQL", () => Command.sp_executesql()))
            using (DbDataReader reader = Executor.UnsafeExecuteDataReader(Command))
            {
                ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdElement>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdElement>>(reader, ProjectorExpression, lookups, retriever, CancellationToken.None);

                IEnumerable<KeyValuePair<K, MList<V>.RowIdElement>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, MList<V>.RowIdElement>>(enumerator);

                try
                {
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);
                    foreach (var kvp in requests)
                    {
                        var results = lookUp[kvp.Key];

                        ((IMListPrivate<V>)kvp.Value).InnerList.AddRange(results);
                        ((IMListPrivate<V>)kvp.Value).InnerListModified(results.Select(a => a.Element).ToList(), null);
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

        public async Task FillAsync(Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever, CancellationToken token)
        {
            Dictionary<K, MList<V>> requests = (Dictionary<K, MList<V>>)lookups.TryGetC(Token);

            if (requests == null)
                return;

            using (HeavyProfiler.Log("SQL", () => Command.sp_executesql()))
            using (DbDataReader reader = await Executor.UnsafeExecuteDataReaderAsync(Command, token: token))
            {
                ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdElement>> enumerator = new ProjectionRowEnumerator<KeyValuePair<K, MList<V>.RowIdElement>>(reader, ProjectorExpression, lookups, retriever, token);

                IEnumerable<KeyValuePair<K, MList<V>.RowIdElement>> enumerabe = new ProjectionRowEnumerable<KeyValuePair<K, MList<V>.RowIdElement>>(enumerator);

                try
                {
                    var lookUp = enumerabe.ToLookup(a => a.Key, a => a.Value);
                    foreach (var kvp in requests)
                    {
                        var results = lookUp[kvp.Key];

                        ((IMListPrivate<V>)kvp.Value).AssignMList(results.ToList());
                        ((IMListPrivate<V>)kvp.Value).InnerListModified(results.Select(a => a.Element).ToList(), null);
                        retriever.ModifiablePostRetrieving(kvp.Value);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
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
                using (var retriever = EntityCache.NewRetriever())
                {
                    if (EagerProjections.Any() || LazyChildProjections.Any())
                        lookups = new Dictionary<LookupToken, IEnumerable>();

                    foreach (var child in EagerProjections)
                        child.Fill(lookups, retriever);

                    using (HeavyProfiler.Log("SQL", () => MainCommand.sp_executesql()))
                    using (DbDataReader reader = Executor.UnsafeExecuteDataReader(MainCommand))
                    {
                        ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(reader, ProjectorExpression, lookups, retriever, CancellationToken.None);

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

                    foreach (var child in LazyChildProjections)
                        child.Fill(lookups, retriever);

                    retriever.CompleteAll();
                }
            
                return tr.Commit(result);
            }
        }

        public async Task<object> ExecuteAsync(CancellationToken token)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                object result;
                using (var retriever = EntityCache.NewRetriever())
                {
                    if (EagerProjections.Any() || LazyChildProjections.Any())
                        lookups = new Dictionary<LookupToken, IEnumerable>();

                    foreach (var child in EagerProjections)
                        child.Fill(lookups, retriever);

                    using (HeavyProfiler.Log("SQL", () => MainCommand.sp_executesql()))
                    using (DbDataReader reader = await Executor.UnsafeExecuteDataReaderAsync(MainCommand, token: token))
                    {
                        ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(reader, ProjectorExpression, lookups, retriever, token);

                        IEnumerable<T> enumerable = new ProjectionRowEnumerable<T>(enumerator);

                        try
                        {
                            if (Unique == null)
                                result = enumerable.ToList();
                            else
                                result = UniqueMethod(enumerable, Unique.Value);
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            FieldReaderException fieldEx = enumerator.Reader.CreateFieldReaderException(ex);
                            fieldEx.Command = MainCommand;
                            fieldEx.Row = enumerator.Row;
                            fieldEx.Projector = ProjectorExpression;
                            throw fieldEx;
                        }
                    }

                    foreach (var child in LazyChildProjections)
                        child.Fill(lookups, retriever);

                    retriever.CompleteAll();
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
                SqlPreCommand eager = EagerProjections?.Select(cp => cp.Command).Combine(Spacing.Double);

                SqlPreCommand lazy = LazyChildProjections?.Select(cp => cp.Command).Combine(Spacing.Double);

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
