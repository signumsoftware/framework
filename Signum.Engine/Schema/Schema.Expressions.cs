using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Server;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps
{

    public partial class Table
    {
        internal Expression CreateBinding(ProjectionToken token, string tableAlias, FieldInfo fi, BinderTools tools)
        {
            EntityField field = Fields.TryGetC(fi.Name);
            if (field == null)
                throw new InvalidOperationException("The field {0} is not included".Formato(fi.Name));

            Expression result = field.Field.GetExpression(token, tableAlias, tools);

            return result;
        }

        internal Expression GetProjectorExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            if (!IsView)
            {
                Schema.Current.AssertAllowed(Type);

                Expression id = this.CreateBinding(token, tableAlias, FieldInitExpression.IdField, tools);
                return new FieldInitExpression(this.Type, tableAlias, id, null, token)
                {
                    Bindings = { new FieldBinding(FieldInitExpression.IdField, id) }
                };
            }
            else
            {
                var bindings = (from kvp in this.Fields
                                let fi = kvp.Value.FieldInfo
                                select new FieldBinding(fi, kvp.Value.Field.GetExpression(token, tableAlias, tools))).ToReadOnly();

                return new EmbeddedFieldInitExpression(this.Type, null, bindings, null);
            }
        }
    }

    public partial class RelationalTable
    {

        internal ColumnExpression RowIdExpression(string tableAlias)
        {
            return new ColumnExpression(typeof(int), tableAlias, ((IColumn)this.PrimaryKey).Name);
        }

        internal ColumnExpression BackColumnExpression(string tableAlias)
        {
            return new ColumnExpression(BackReference.ReferenceType(), tableAlias, BackReference.Name);
        }

        internal Expression FieldExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            return Field.GetExpression(token, tableAlias, tools);
        }

        internal Expression GetProjectorExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            Schema.Current.AssertAllowed(this.BackReference.ReferenceTable.Type);

            Type elementType = typeof(MListElement<,>).MakeGenericType(BackReference.FieldType, Field.FieldType);

            return new MListElementExpression(
                 RowIdExpression(tableAlias) ,
                (FieldInitExpression)this.BackReference.GetExpression(token, tableAlias, tools),
                this.Field.GetExpression(token, tableAlias, tools), this);
        }
    }

    public static partial class ColumnExtensions
    {
        public static Type ReferenceType(this IColumn columna)
        {
            Debug.Assert(columna.SqlDbType == SqlBuilder.PrimaryKeyType);

            return columna.Nullable ? typeof(int?) : typeof(int);
        }
    }

    public abstract partial class Field
    {
        internal abstract Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools);
    }

    public partial class FieldPrimaryKey
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            return new ColumnExpression(typeof(int), tableAlias, this.Name);
        }
    }

    public partial class FieldValue
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            return new ColumnExpression(this.FieldType, tableAlias, this.Name);
        }
    }

    public partial class FieldReference
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            Type cleanType = IsLite ? Reflector.ExtractLite(FieldType) : FieldType;

            var result = new FieldInitExpression(cleanType, null,
                new ColumnExpression(this.ReferenceType(), tableAlias, Name), null, token);

            if(this.IsLite)
                return tools.MakeLite(this.FieldType, result, null);
            else 
                return result; 
        }
    }

    public partial class FieldEnum
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            return Expression.Convert(new ColumnExpression(this.ReferenceType(), tableAlias, Name), FieldType);
        }
    }

    public partial class FieldMList
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            return new MListExpression(FieldType, null, RelationalTable); // keep back id empty for some seconds 
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            var bindings = (from kvp in EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi, kvp.Value.Field.GetExpression(token, tableAlias, tools))).ToReadOnly();

            ColumnExpression hasValue = HasValue == null ? null : new ColumnExpression(typeof(bool), tableAlias, HasValue.Name);
            return new EmbeddedFieldInitExpression(this.FieldType, hasValue, bindings, this); 
        }

        internal EmbeddedFieldInitExpression GetConstantExpression(object contant, QueryBinder tools)
        {
            var bindings = (from kvp in EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi,
                                tools.VisitConstant(kvp.Value.Getter(contant), kvp.Value.FieldInfo.FieldType))).ToReadOnly();

            return new EmbeddedFieldInitExpression(this.FieldType, Expression.Constant(true), bindings, this); 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            var implementations = (from kvp in ImplementationColumns
                                   select new ImplementationColumnExpression(kvp.Key,
                                            new FieldInitExpression(kvp.Key, null,
                                                new ColumnExpression(kvp.Value.ReferenceType(), tableAlias, kvp.Value.Name),
                                                null, token))).ToReadOnly();

            var result = new ImplementedByExpression(IsLite ? Reflector.ExtractLite(FieldType) : FieldType, implementations);

            if (this.IsLite)
                return tools.MakeLite(this.FieldType, result, null);
            else
                return result; 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override Expression GetExpression(ProjectionToken token, string tableAlias, BinderTools tools)
        {
            Expression result = new ImplementedByAllExpression(IsLite ? Reflector.ExtractLite(FieldType) : FieldType,
                new ColumnExpression(Column.ReferenceType(), tableAlias, Column.Name),
                new ColumnExpression(Column.ReferenceType(), tableAlias, ColumnTypes.Name), token);

            if (this.IsLite)
                return tools.MakeLite(this.FieldType, result, null);
            else
                return result; 
        }
    }
}
