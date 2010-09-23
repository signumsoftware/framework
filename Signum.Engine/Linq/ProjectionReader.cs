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
using Signum.Engine.Properties;
using System.Data.SqlClient;

namespace Signum.Engine.Linq
{
    internal interface IProjectionRow
    {
        FieldReader Reader { get; }

        IProjectionRow Parent { get; }

        Retriever Retriever { get; }

        MList<S> GetList<S>(RelationalTable tr, int id);

        S GetIdentifiable<S>(int? id) where S : IdentifiableEntity;
        S GetImplementedBy<S>(Type[] types, params int?[] ids) where S :  IIdentifiable;
        S GetImplementedByAll<S>(int? id, int? typeId) where S :  IIdentifiable;

        Lite<S> GetLiteIdentifiable<S>(int? id, int? typeId, string str) where S : class, IIdentifiable;
        Lite<S> GetLiteImplementedByAll<S>(int? id, int? typeId) where S :class,  IIdentifiable;
    }

    internal class ProjectionRowEnumerator<T> : IProjectionRow, IEnumerator<T>
    {
        public FieldReader Reader { get; private set;}

        public IProjectionRow Parent { get; private set; }

        SqlDataReader reader;

        T current;
        Func<IProjectionRow, T> projector; 
        Expression<Func<IProjectionRow, T>> projectorExpression;

        Retriever retriever;

        internal ProjectionRowEnumerator(SqlDataReader reader, Expression<Func<IProjectionRow, T>> projectorExpression, IProjectionRow parent, Retriever retriever)
        {
            this.reader = reader;
            this.Reader = new FieldReader(reader);

            this.projectorExpression = projectorExpression;
            this.projector = projectorExpression.Compile();
            this.Parent = parent;
            this.retriever = retriever;
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
            if (reader.Read())
            {
                this.current = this.projector(this);
                return true;
            }
            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            reader.Dispose();

            if (retriever != null)
            {
                retriever.ProcessAll();
            }
        }

        public Retriever Retriever
        {
            get { return retriever; }
        }
  
        public MList<S> GetList<S>(RelationalTable tr, int id)
        {
            return (MList<S>)Retriever.GetList(tr, id);
        }

        public S GetIdentifiable<S>(int? id) where S : IdentifiableEntity
        {
            if (id == null) return null; 
            return (S)Retriever.GetIdentifiable(ConnectionScope.Current.Schema.Table(typeof(S)), id.Value, true);
        }

        public S GetImplementedBy<S>(Type[] types, params int?[] ids)
            where S :  IIdentifiable
        {
            int pos = ids.IndexOf(id => id.HasValue);
            if (pos == -1) return default(S);
            return (S)(object)Retriever.GetIdentifiable(ConnectionScope.Current.Schema.Table(types[pos]), ids[pos].Value, true);
        }

        public S GetImplementedByAll<S>(int? id, int? idType)
            where S :  IIdentifiable
        {
            if (id == null) return default(S); 
            Table table = Schema.Current.TablesForID[idType.Value];
            return (S)(object)Retriever.GetIdentifiable(table, id.Value, true);
        }

        public Lite<S> GetLiteIdentifiable<S>( int? id, int? idType, string str) where S : class, IIdentifiable
        {
            if (id == null) return null;

            Type runtimeType = Schema.Current.TablesForID[idType.Value].Type;
            return new Lite<S>(runtimeType, id.Value) { ToStr = str }; 
        }

        public Lite<S> GetLiteImplementedByAll<S>(int? id, int? idType) where S : class, IIdentifiable
        {
            if (id == null) return null; 
            Table table = Schema.Current.TablesForID[idType.Value];
            return (Lite<S>)Retriever.GetLite(table, typeof(S), id.Value);
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
                .ThrowIfNullC("Cannot enumerate more than once");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
