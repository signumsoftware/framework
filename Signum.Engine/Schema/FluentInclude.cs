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
    }

}
