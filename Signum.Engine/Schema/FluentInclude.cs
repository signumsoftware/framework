using Signum.Engine.Maps;
using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Maps
{
    public class FluentInclude<T> where T : Entity
    {
        public SchemaBuilder SchemaBuilder { get; private set; }
        public Table Table { get; private set; }

        public FluentInclude(Table table, SchemaBuilder schemaBuilder)
        {
            Table = table;
            SchemaBuilder = schemaBuilder;
        }

        public FluentInclude<T> WithUniqueIndex(Expression<Func<T, object>> fields, Expression<Func<T, bool>> where = null, Expression<Func<T, object>> includeFields = null)
        {
            this.SchemaBuilder.AddUniqueIndex<T>(fields, where, includeFields);
            return this;
        }

        public FluentInclude<T> WithIndex(Expression<Func<T, object>> fields, Expression<Func<T, bool>> where = null, Expression<Func<T, object>> includeFields = null)
        {
            this.SchemaBuilder.AddIndex<T>(fields, where, includeFields);
            return this;
        }

        public FluentInclude<T> WithUniqueIndexMList<M>(Expression<Func<T, MList<M>>> mlist, Expression<Func<MListElement<T, M>, object>> fields = null, Expression<Func<MListElement<T, M>, bool>> where = null, Expression<Func<MListElement<T, M>, object>> includeFields = null)
        {
            if (fields == null)
                fields = mle => new { mle.Parent, mle.Element };

            this.SchemaBuilder.AddUniqueIndexMList<T, M>(mlist, fields, where, includeFields);
            return this;
        }

        public FluentInclude<T> WithIndexMList<M>(Expression<Func<T, MList<M>>> mlist, Expression<Func<MListElement<T, M>, object>> fields, Expression<Func<MListElement<T, M>, bool>> where = null, Expression<Func<MListElement<T, M>, object>> includeFields = null)
        {
            this.SchemaBuilder.AddIndexMList<T, M>(mlist, fields, where, includeFields);
            return this;
        }

    }

}
