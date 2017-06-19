using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Data.Common;

namespace Signum.Engine.Linq
{
    public class LookupToken
    {
        public override string ToString()
        {
            return this.GetHashCode().ToString();
        }
    }

    public interface IProjectionRow
    {
        FieldReader Reader { get; }

        IRetriever Retriever { get; }

        IEnumerable<S> Lookup<K, S>(LookupToken token, K key);
        MList<S> LookupRequest<K, S>(LookupToken token, K key, MList<S> field);
    }

    internal class ProjectionRowEnumerator<T> : IProjectionRow, IEnumerator<T>
    {
        public FieldReader Reader { get; private set;}
        public IRetriever Retriever { get; private set; }

        public IProjectionRow Parent { get; private set; }

        public CancellationToken Token { get; private set; }

        DbDataReader dataReader;

        T current;
        Func<IProjectionRow, T> projector; 
        Expression<Func<IProjectionRow, T>> projectorExpression;

        Dictionary<LookupToken, IEnumerable> lookups;

        internal ProjectionRowEnumerator(DbDataReader dataReader, Expression<Func<IProjectionRow, T>> projectorExpression, Dictionary<LookupToken, IEnumerable> lookups, IRetriever retriever, CancellationToken token)
        {
            this.dataReader = dataReader;
            this.Reader = new FieldReader(dataReader);

            this.projectorExpression = ExpressionCompilableAsserter.Assert(projectorExpression);
            this.projector = projectorExpression.Compile();
            this.lookups = lookups;
            this.Row = -1;
            this.Retriever = retriever;
            this.Token = token;
        }

        public T Current
        {
            get { return this.current; }
        }

        object IEnumerator.Current
        {
            get { return this.current; }
        }

        public int Row;
        public bool MoveNext()
        {
            Token.ThrowIfCancellationRequested();

            if (dataReader.Read())
            {
                this.Row++;
                this.current = this.projector(this); //InvalidOperationException? Press F5
                return true;
            }
            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }

        public IEnumerable<S> Lookup<K, S>(LookupToken token, K key)
        {
            Lookup<K, S> lookup = (Lookup<K, S>)lookups[token];

            if (!lookup.Contains(key))
                return Enumerable.Empty<S>();
            else
                return lookup[key];
        }

        public MList<S> LookupRequest<K, S>(LookupToken token, K key, MList<S> field)
        {
            Dictionary<K, MList<S>> dictionary = (Dictionary<K, MList<S>>)lookups.GetOrCreate(token, () => (IEnumerable)new Dictionary<K, MList<S>>());

            return dictionary.GetOrCreate(key, () => field != null && field.Count == 0 ? field : new MList<S>());
        }
    }

    internal class ProjectionRowEnumerable<T> : IEnumerable<T>, IEnumerable
    {
        ProjectionRowEnumerator<T> enumerator;

        internal ProjectionRowEnumerable(ProjectionRowEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Interlocked.Exchange(ref enumerator, null)
                .ThrowIfNull("Cannot enumerate more than once");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class ExpressionCompilableAsserter : ExpressionVisitor
    {
        public override Expression Visit(Expression exp)
        {
            try
            {
                return base.Visit(exp);
            }
            catch (ArgumentException e)
            {
                if (e.Message.Contains("reducible"))
                    throw new NotSupportedException("The expression can not be compiled:\r\n{0}".FormatWith(exp.ToString()));
                throw;
            }
        }

        internal static Expression<Func<IProjectionRow, T>> Assert<T>(Expression<Func<IProjectionRow, T>> projectorExpression)
        {
            return (Expression<Func<IProjectionRow, T>>)new ExpressionCompilableAsserter().Visit(projectorExpression);
        }
    }
}
