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
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder)
        {
            if (IsView)
            {
                var bindings = this.Fields.Values.Select(ef=>new FieldBinding(ef.FieldInfo, ef.Field.GetExpression(tableAlias, binder, null))).ToReadOnly();

                return new EmbeddedEntityExpression(this.Type, null, bindings, null);
            }
            else
            {
                Expression id = GetIdExpression(tableAlias);

                var bindings = GenerateBindings(tableAlias, binder, id); 
                var mixins = GenerateMixins(tableAlias, binder, id);

                Schema.Current.AssertAllowed(Type);

                var result = new EntityExpression(this.Type, id, tableAlias, bindings, mixins, avoidExpandOnRetrieving: false);

                return result; 
            }
        }

        internal ReadOnlyCollection<FieldBinding> GenerateBindings(Alias tableAlias, QueryBinder binder, Expression id)
        {
            List<FieldBinding> result = new List<FieldBinding>();

           result.Add(new FieldBinding(Table.fiId, id));

            foreach (var ef in this.Fields.Values)
            {
                var fi = ef.FieldInfo;

                if (!ReflectionTools.FieldEquals(fi, fiId))
                    result.Add(new FieldBinding(fi, ef.Field.GetExpression(tableAlias, binder, id)));
            }


            return result.ToReadOnly();
        }

        internal ReadOnlyCollection<MixinEntityExpression> GenerateMixins(Alias tableAlias, QueryBinder binder, Expression id)
        {
            if (this.Mixins == null)
                return null;

            return this.Mixins.Values.Select(m => (MixinEntityExpression)m.GetExpression(tableAlias, binder, id)).ToReadOnly();
        }


        internal Expression GetIdExpression(Alias alias)
        {
            var field = Fields.TryGetC(Table.fiId.Name);

            if (field == null)
            {
                field = Fields.Values.FirstOrDefault(f => f .Field is IColumn && ((IColumn)f.Field).PrimaryKey);
                if (field == null)
                    return null;
            }

            return field.Field.GetExpression(alias, null, null);
        }

        ColumnExpression ITablePrivate.GetPrimaryOrder(Alias alias)
        {
            return (ColumnExpression)GetIdExpression(alias);
        }
    }

    public partial class RelationalTable
    {
        internal ColumnExpression RowIdExpression(Alias tableAlias)
        {
            return new ColumnExpression(typeof(int), tableAlias, ((IColumn)this.PrimaryKey).Name);
        }

        internal ColumnExpression BackColumnExpression(Alias tableAlias)
        {
            return new ColumnExpression(BackReference.ReferenceType(), tableAlias, BackReference.Name);
        }

        internal Expression FieldExpression(Alias tableAlias, QueryBinder binder)
        {
            return Field.GetExpression(tableAlias, binder, null);
        }

        internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder)
        {
            Schema.Current.AssertAllowed(this.BackReference.ReferenceTable.Type);

            Type elementType = typeof(MListElement<,>).MakeGenericType(BackReference.FieldType, Field.FieldType);

            return new MListElementExpression(
                 RowIdExpression(tableAlias) ,
                (EntityExpression)this.BackReference.GetExpression(tableAlias, binder, null),
                this.Field.GetExpression(tableAlias, binder, null), this);
        }

        ColumnExpression ITablePrivate.GetPrimaryOrder(Alias alias)
        {
            return RowIdExpression(alias);
        }
    }

    public static partial class ColumnExtensions
    {
        public static Type ReferenceType(this IColumn column)
        {
            Debug.Assert(column.SqlDbType == SqlBuilder.PrimaryKeyType);

            return column.Nullable ? typeof(int?) : typeof(int);
        }
    }

    public abstract partial class Field
    {
        internal abstract Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id);
    }

    public partial class FieldPrimaryKey
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            return new ColumnExpression(typeof(int), tableAlias, this.Name);
        }
    }

    public partial class FieldValue
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            return new ColumnExpression(this.FieldType, tableAlias, this.Name);
        }
    }

    public partial class FieldReference
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            Type cleanType = IsLite ? Lite.Extract(FieldType) : FieldType;

            var result = new EntityExpression(cleanType, new ColumnExpression(this.ReferenceType(), tableAlias, Name), null, null, null, AvoidExpandOnRetrieving);

            if(this.IsLite)
                return binder.MakeLite(result, null);
            else 
                return result; 
        }
    }

    public partial class FieldEnum
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            return Expression.Convert(new ColumnExpression(this.ReferenceType(), tableAlias, Name), FieldType);
        }
    }

    public partial class FieldMList
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            return new MListExpression(FieldType, id, RelationalTable); // keep back id empty for some seconds 
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var bindings = (from kvp in EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder, id))).ToReadOnly();

            ColumnExpression hasValue = HasValue == null ? null : new ColumnExpression(typeof(bool), tableAlias, HasValue.Name);
            return new EmbeddedEntityExpression(this.FieldType, hasValue, bindings, this); 
        }
    }

    public partial class FieldMixin
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var bindings = (from kvp in Fields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder, id))).ToReadOnly();

            return new MixinEntityExpression(this.FieldType, bindings, this);
        }

        //internal EmbeddedEntityExpression FromMemberInitiExpression(MemberInitExpression mie, QueryBinder tools)
        //{
        //    var dic = mie.Bindings.OfType<MemberAssignment>().ToDictionary(a => (a.Member as FieldInfo ?? Reflector.FindFieldInfo(mie.Type, (PropertyInfo)a.Member)).Name, a => a.Expression);

        //    var bindings = (from kvp in EmbeddedFields
        //                    let fi = kvp.Value.FieldInfo
        //                    select new FieldBinding(fi,
        //                        !(fi.FieldType.IsByRef || fi.FieldType.IsNullable()) ? dic.GetOrThrow(fi.Name, "No value defined for non-nullable field {0}") :
        //                        (dic.TryGetC(fi.Name) ?? tools.VisitConstant(Expression.Constant(null, fi.FieldType), fi.FieldType))
        //                    )).ToReadOnly();

        //    return new EmbeddedEntityExpression(this.FieldType, Expression.Constant(true), bindings, this);
        //}
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var implementations = ImplementationColumns.SelectDictionary(t => t, (t, ic) =>
                 new EntityExpression(t, new ColumnExpression(ic.ReferenceType(), tableAlias, ic.Name), null, null, null,  AvoidExpandOnRetrieving));

            var result = new ImplementedByExpression(IsLite ? Lite.Extract(FieldType) : FieldType, SplitStrategy, implementations);

            if (this.IsLite)
                return binder.MakeLite(result, null);
            else
                return result; 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            Expression result = new ImplementedByAllExpression(IsLite ? Lite.Extract(FieldType) : FieldType,
                new ColumnExpression(Column.ReferenceType(), tableAlias, Column.Name),
                new TypeImplementedByAllExpression(new ColumnExpression(Column.ReferenceType(), tableAlias, ColumnTypes.Name)));

            if (this.IsLite)
                return binder.MakeLite(result, null);
            else
                return result;
        }
    }
}
