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
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder)
        {
            Expression id = GetIdExpression(tableAlias);

            if (IsView)
            {
                var bindings = this.Fields.Values.Select(ef=>new FieldBinding(ef.FieldInfo, ef.Field.GetExpression(tableAlias, binder, id))).ToReadOnly();

                var hasValue = id == null ? Expression.Constant(true): SmartEqualizer.NotEqualNullable(id, Expression.Constant(null, id.Type.Nullify()));

                return new EmbeddedEntityExpression(this.Type, hasValue, bindings, null);
            }
            else
            {
                var bindings = GenerateBindings(tableAlias, binder, id); 
                var mixins = GenerateMixins(tableAlias, binder, id);

                Schema.Current.AssertAllowed(Type, inUserInterface: false);

                var result = new EntityExpression(this.Type, (PrimaryKeyExpression)id, tableAlias, bindings, mixins, avoidExpandOnRetrieving: false);

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
            var res = GetIdExpression(alias);

            return res is PrimaryKeyExpression ? 
                (ColumnExpression)((PrimaryKeyExpression)res).Value:
                (ColumnExpression)res;
        }
    }

    public partial class TableMList
    {
        internal PrimaryKeyExpression RowIdExpression(Alias tableAlias)
        {
            var primary = (IColumn)this.PrimaryKey;

            return new PrimaryKeyExpression(new ColumnExpression(primary.Type.Nullify(), tableAlias, primary.Name));
        }

        internal PrimaryKeyExpression BackColumnExpression(Alias tableAlias)
        {
            return new PrimaryKeyExpression(new ColumnExpression(BackReference.Type.Nullify(), tableAlias, BackReference.Name));
        }

        internal ColumnExpression OrderExpression(Alias tableAlias)
        {
            return new ColumnExpression(typeof(int), tableAlias, ((IColumn)this.Order).Name);
        }

        internal Expression FieldExpression(Alias tableAlias, QueryBinder binder, bool withRowId)
        {
            var rowId = RowIdExpression(tableAlias);

            var exp = Field.GetExpression(tableAlias, binder, rowId);

            if (!withRowId)
                return exp;

            var type = this.Field.FieldType;

            var ci = typeof(MList<>.RowIdValue).MakeGenericType(type).GetConstructor(new[] { type, typeof(PrimaryKey), typeof(int?) });

            var order =  Order == null ? (Expression)Expression.Constant(null, typeof(int?)) : OrderExpression(tableAlias).Nullify();

            return Expression.New(ci, exp, rowId.UnNullify(), order);
        }

        internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder)
        {
            Schema.Current.AssertAllowed(this.BackReference.ReferenceTable.Type, inUserInterface: false);

            Type elementType = typeof(MListElement<,>).MakeGenericType(BackReference.FieldType, Field.FieldType);

            var rowId = RowIdExpression(tableAlias);

            return new MListElementExpression(
                rowId,
                (EntityExpression)this.BackReference.GetExpression(tableAlias, binder, null),
                this.Order == null ? null: OrderExpression(tableAlias), 
                this.Field.GetExpression(tableAlias, binder, rowId), this);
        }

        ColumnExpression ITablePrivate.GetPrimaryOrder(Alias alias)
        {
            return (ColumnExpression)RowIdExpression(alias).Value;
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
            return new PrimaryKeyExpression(new ColumnExpression(this.Type.Nullify(), tableAlias, this.Name).Nullify());
        }
    }

    public partial class FieldValue
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var column = new ColumnExpression(this.Type, tableAlias, this.Name);

            if(this.Type == this.FieldType)
                return column;

            return Expression.Convert(column, this.FieldType);
        }
    }

    public partial class FieldTicks
    {
        public static readonly PropertyInfo piDateTimeTicks = ReflectionTools.GetPropertyInfo((DateTime d) => d.Ticks);

        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            if (this.Type == this.FieldType)
               return new ColumnExpression(this.Type, tableAlias, this.Name);

            if (this.Type == typeof(DateTime))
                return Expression.Property(new ColumnExpression(this.Type, tableAlias, this.Name), piDateTimeTicks);

            throw new NotImplementedException("FieldTicks of type {0} not supported".FormatWith(this.Type));
        }
    }

    public partial class FieldReference
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            Type cleanType = IsLite ? Lite.Extract(FieldType) : FieldType;

            var result = new EntityExpression(cleanType, new PrimaryKeyExpression(new ColumnExpression(this.Type.Nullify(), tableAlias, Name)), null, null, null, AvoidExpandOnRetrieving);

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
            return Expression.Convert(new ColumnExpression(this.Type, tableAlias, Name), FieldType);
        }
    }

    public partial class FieldMList
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            return new MListExpression(FieldType, id, TableMList); // keep back id empty for some seconds 
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var bindings = (from kvp in EmbeddedFields
                            let fi = kvp.Value.FieldInfo
                            select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder, id))).ToReadOnly();

            Expression hasValue = HasValue == null ? SmartEqualizer.NotEqualNullable(id,
                id is PrimaryKeyExpression ? QueryBinder.NullId(((PrimaryKeyExpression)id).ValueType) : (Expression)Expression.Constant(null, id.Type.Nullify())) :
                new ColumnExpression(((IColumn)HasValue).Type, tableAlias, HasValue.Name);

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

            return new MixinEntityExpression(this.FieldType, bindings, tableAlias, this);
        }
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id)
        {
            var implementations = ImplementationColumns.SelectDictionary(t => t, (t, ic) =>
                 new EntityExpression(t, new PrimaryKeyExpression(new ColumnExpression(ic.Type.Nullify(), tableAlias, ic.Name)), null, null, null, AvoidExpandOnRetrieving));

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
                new ColumnExpression(Column.Type, tableAlias, Column.Name),
                new TypeImplementedByAllExpression(new PrimaryKeyExpression(new ColumnExpression(ColumnType.Type.Nullify(), tableAlias, ColumnType.Name))));

            if (this.IsLite)
                return binder.MakeLite(result, null);
            else
                return result;
        }
    }
}
