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

namespace Signum.Engine.Linq
{
    internal interface IProjectionRow
    {
        S GetValue<S>(string alias, string name);
        IEnumerable<S> ExecuteSubQuery<S>(TranslateResult<S> tr);

        Retriever Retriever { get; }

        MList<S> GetList<S>(RelationalTable tr, int id);

        S GetIdentificable<S>(int? id) where S : IdentifiableEntity;
        S GetImplementedBy<S>(Type[] types, params int?[] ids) where S :  IIdentifiable;
        S GetImplementedByAll<S>(int? id, int? idType) where S :  IIdentifiable;

        Lazy<S> GetLazyIdentificable<S>(Type runtimeType, int? id, string str) where S : class, IIdentifiable;
        Lazy<S> GetLazyImplementedBy<S>(Type[] types, params int?[] ids) where S :class,  IIdentifiable;
        Lazy<S> GetLazyImplementedByAll<S>(int? id, int? idType) where S :class,  IIdentifiable;
    }

    internal class ProjectionRowEnumerator<T> : IProjectionRow, IEnumerator<T>
    {
        DataTable dt;
        DataRow currentRow; 
        int currentIndex = 0; 

        T current;
        Func<IProjectionRow, T> projector; 
        Expression<Func<IProjectionRow, T>> projectorExpression;
        DbQueryProvider provider;

        Retriever retriever;
        EntityCache objectCache; 


        IProjectionRow previous;
        string alias;

        internal ProjectionRowEnumerator(DataTable dt, Expression<Func<IProjectionRow, T>> projectorExpression, DbQueryProvider provider, bool HasFullObjects, IProjectionRow previous, string alias)
        {
            this.dt = dt;
            this.projectorExpression = projectorExpression;
            this.projector = projectorExpression.Compile();
            this.provider = provider;
            this.previous = previous;
           
            this.alias = alias;

            if (HasFullObjects && previous == null)
            {
                this.retriever = new Retriever();
                this.objectCache = new EntityCache(); 
            }
        }

        public S GetValue<S>(string alias, string name)
        {
            if (this.alias == alias)
            {
                S result = ReflectionTools.ChangeType<S>(currentRow.IsNull(name) ? null: currentRow[name]);

                return result;
            }
            return previous.GetValue<S>(alias, name); 
        }

        public IEnumerable<S> ExecuteSubQuery<S>(TranslateResult<S> tr)
        {
            return provider.ExecuteReader(tr, this).ToList();
        }

        public T Current
        {
            get { return this.current; }
        }

        object IEnumerator.Current
        {
            get { return this.current; }
        }

        public bool MoveNext()
        {
            if (currentIndex <  dt.Rows.Count)
            {
                currentRow = dt.Rows[currentIndex];
                this.current = this.projector(this);
                currentIndex++; 
                return true;
            }
            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            if (retriever != null)
            {
                retriever.ProcessAll();
            }

            if (objectCache != null)
            {
                objectCache.Dispose(); 
            }
        }

        public Retriever Retriever
        {
            get { return retriever ?? previous.Retriever; }
        }
  
        public MList<S> GetList<S>(RelationalTable tr, int id)
        {
            return (MList<S>)Retriever.GetList(tr, id);
        }

        public S GetIdentificable<S>(int? id) where S : IdentifiableEntity
        {
            if (id == null) return null; 
            return (S)Retriever.GetIdentifiable(ConnectionScope.Current.Schema.Table(typeof(S)), id.Value);
        }

        public S GetImplementedBy<S>(Type[] types, params int?[] ids)
            where S :  IIdentifiable
        {
            int pos = ids.IndexOf(id => id.HasValue);
            if (pos == -1) return default(S); 
            return (S)(object)Retriever.GetIdentifiable(ConnectionScope.Current.Schema.Table(types[pos]), ids[pos].Value);
        }

        public S GetImplementedByAll<S>(int? id, int? idType)
            where S :  IIdentifiable
        {
            if (id == null) return default(S); 
            Table table = Schema.Current.TablesForID[idType.Value];
            return (S)(object)Retriever.GetIdentifiable(table, id.Value);
        }

        public Lazy<S> GetLazyIdentificable<S>(Type runtimeType, int? id, string str) where S : class, IIdentifiable
        {
            if (id == null) return null;
            return new Lazy<S>(runtimeType, id.Value) { ToStr = str }; 
            //return (Lazy<S>)Retriever.GetLazy(ConnectionScope.Current.Schema.Tables[runtimeType], typeof(S), id.Value);
        }

        public Lazy<S> GetLazyImplementedBy<S>(Type[] types, params int?[] ids) where S : class, IIdentifiable
        {
            int pos = ids.IndexOf(id => id.HasValue);
            if (pos == -1) return null;
            Table table = Schema.Current.Table(types[pos]);
            return (Lazy<S>)Retriever.GetLazy(table, typeof(S), ids[pos].Value);
        }

        public Lazy<S> GetLazyImplementedByAll<S>(int? id, int? idType) where S : class, IIdentifiable
        {
            if (id == null) return null; 
            Table table = Schema.Current.TablesForID[idType.Value];
            return (Lazy<S>)Retriever.GetLazy(table, typeof(S), id.Value);
        }
    }

    /// <summary>
    /// ProjectionReader is an implemention of IEnumerable that converts data from DbDataReader into
    /// objects via a projector function,
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ProjectionRowReader<T> : IEnumerable<T>, IEnumerable
    {
        ProjectionRowEnumerator<T> enumerator;

        internal ProjectionRowReader(ProjectionRowEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Interlocked.Exchange(ref enumerator, null)
                .ThrowIfNullC(Signum.Engine.Properties.Resources.CannotEnumerateMoreThanOnce);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
