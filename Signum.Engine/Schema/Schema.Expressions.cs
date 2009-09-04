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
        internal Expression CreateBinding(string tableAlias, FieldInfo fi, QueryBinder binder)
        {
            EntityField field = Fields.TryGetC(fi.Name);
            if (field == null)
                throw new ApplicationException(Resources.TheField0IsNotIncluded.Formato(fi.Name));

            Expression result = field.Field.GetExpression(tableAlias, binder);

            return result;
        }
    }

    public partial class RelationalTable
    {
        internal ColumnExpression BackColumnExpression(string tableAlias)
        {
            return new ColumnExpression(BackReference.ReferenceType(), tableAlias, BackReference.Name);
        }

        internal Expression FieldExpression(string tableAlias, QueryBinder binder)
        {
            return Field.GetExpression(tableAlias, binder);
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
        internal abstract Expression GetExpression(string tableAlias, QueryBinder binder);
    }

    public partial class FieldPrimaryKey
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            return new ColumnExpression(typeof(int), tableAlias, this.Name);
        }
    }

    public partial class FieldValue
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            return new ColumnExpression(this.FieldType, tableAlias, this.Name);
        }
    }

    public static partial class ReferenceFieldExtensions
    {  
    }


    public partial class FieldReference
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            var result = new FieldInitExpression(IsLazy ? Reflector.ExtractLazy(FieldType) : FieldType, null,
                new ColumnExpression(this.ReferenceType(), tableAlias, Name), null);

            if(this.IsLazy)
                return binder.MakeLazy(this.FieldType, result);
            else 
                return result; 
        }
    }

    public partial class FieldEnum
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            return Expression.Convert(new ColumnExpression(this.ReferenceType(), tableAlias, Name), FieldType);
        }
    }

    public partial class FieldMList
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            return new MListExpression(FieldType, null, RelationalTable); // keep back id empty for some seconds 
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            var bindings = (from kvp in EmbeddedFields
                            let fi = FieldType.GetField(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder))).ToReadOnly(); 
                                          
            return new EmbeddedFieldInitExpression(this.FieldType, bindings); 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            var implementations = (from kvp in ImplementationColumns
                                   select new ImplementationColumnExpression(kvp.Key,
                                            new FieldInitExpression(kvp.Key, null,
                                                new ColumnExpression(kvp.Value.ReferenceType(), tableAlias, kvp.Value.Name), null))).ToReadOnly();

            var result = new ImplementedByExpression(IsLazy ? Reflector.ExtractLazy(FieldType) : FieldType, implementations);

            if (this.IsLazy)
                return binder.MakeLazy(this.FieldType, result);
            else
                return result; 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override Expression GetExpression(string tableAlias, QueryBinder binder)
        {
            Expression result = new ImplementedByAllExpression(IsLazy ? Reflector.ExtractLazy(FieldType) : FieldType,
                new ColumnExpression(Column.ReferenceType(), tableAlias, Column.Name),
                new ColumnExpression(Column.ReferenceType(), tableAlias, ColumnTypes.Name));

            if (this.IsLazy)
                return binder.MakeLazy(this.FieldType, result);
            else
                return result; 
        }
    }
}
