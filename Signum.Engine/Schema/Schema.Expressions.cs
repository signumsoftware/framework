using System.Collections.ObjectModel;
using Signum.Engine.Linq;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps;

public partial class Table
{
    internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder, bool disableAssertAllowed = false)
    {
        Expression? id = GetIdExpression(tableAlias);

        if (IsView)
        {
            var bindings = this.Fields.Values.Select(ef => new FieldBinding(ef.FieldInfo, ef.Field.GetExpression(tableAlias, binder, id!, null, null))).ToReadOnly();

            var hasValue = id == null ? Expression.Constant(true): SmartEqualizer.NotEqualNullable(id, Expression.Constant(null, id.Type.Nullify()));

            return new EmbeddedEntityExpression(this.Type, hasValue, bindings, null, null, this, null);
        }
        else
        {
            if(!disableAssertAllowed)
                Schema.Current.AssertAllowed(Type, inUserInterface: false);

            var entityContext = new EntityContextInfo((PrimaryKeyExpression)id!, null);

            var period = GenerateSystemPeriod(tableAlias, binder);
            var bindings = GenerateBindings(tableAlias, binder, id!, period, entityContext);
            var mixins = GenerateMixins(tableAlias, binder, id!, period, entityContext);

            var result = new EntityExpression(this.Type, (PrimaryKeyExpression)id!, period, tableAlias, bindings, mixins, period, avoidExpandOnRetrieving: false);

            return result;
        }
    }

    internal IntervalExpression? GenerateSystemPeriod(Alias tableAlias, QueryBinder binder, bool force = false)
    {
        return this.SystemVersioned != null && (force || binder.systemTime is SystemTime.Interval) ? this.SystemVersioned.IntervalExpression(tableAlias) : null;
    }



    internal ReadOnlyCollection<FieldBinding> GenerateBindings(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        List<FieldBinding> result = new List<FieldBinding>
        {
            new FieldBinding(Table.fiId, id)
        };

        foreach (var ef in this.Fields.Values)
        {
            var fi = ef.FieldInfo;

            if (!ReflectionTools.FieldEquals(fi, fiId))
                result.Add(new FieldBinding(fi, ef.Field.GetExpression(tableAlias, binder, id, period, entityContext)));
        }

        if (this.Type.IsEntity() && entityContext != null)
            result.AddRange(Schema.Current.GetAdditionalQueryBindings(PropertyRoute.Root(this.Type), entityContext, period));

        return result.ToReadOnly();
    }

    internal ReadOnlyCollection<MixinEntityExpression>? GenerateMixins(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? context)
    {
        if (this.Mixins == null)
            return null;

        return this.Mixins.Values.Select(m => (MixinEntityExpression)m.GetExpression(tableAlias, binder, id, period, context)).ToReadOnly();
    }


    internal Expression? GetIdExpression(Alias alias)
    {
        var field = Fields.TryGetC(Table.fiId.Name);

        if (field == null)
        {
            field = GetViewPrimaryKey();
            if (field == null)
                return null;

            if (field.Field is FieldReference fr)
                return new ColumnExpression(Signum.Entities.PrimaryKey.Type(fr.ReferenceTable.Type).Nullify(), alias, fr.Name);
        }

        return field.Field!.GetExpression(alias, null!, null!, null, null);
    }

    public EntityField? GetViewPrimaryKey()
    {
        return Fields.Values.FirstOrDefault(f => f.Field is IColumn column && column.PrimaryKey);
    }

    ColumnExpression ITablePrivate.GetPrimaryOrder(Alias alias)
    {
        var res = GetIdExpression(alias);

        return res is PrimaryKeyExpression pe?
            (ColumnExpression)pe.Value :
            (ColumnExpression)res!;
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
        return new ColumnExpression(typeof(int), tableAlias, ((IColumn)this.Order!).Name);
    }

    internal Expression FieldExpression(Alias tableAlias, QueryBinder binder, IntervalExpression? externalPeriod, bool withRowId)
    {
        var rowId = RowIdExpression(tableAlias);
        var parentId = new PrimaryKeyExpression(new ColumnExpression(this.BackReference.Type.Nullify(), tableAlias, this.BackReference.Name));
        var entityContext = new EntityContextInfo(parentId, rowId);

        var exp = Field.GetExpression(tableAlias, binder, rowId, externalPeriod, entityContext);

        if (!withRowId)
            return exp;

        var type = this.Field.FieldType;

        var ci = typeof(MList<>.RowIdElement).MakeGenericType(type).GetConstructor(new[] { type, typeof(PrimaryKey), typeof(int?) })!;

        var order =  Order == null ? (Expression)Expression.Constant(null, typeof(int?)) : OrderExpression(tableAlias).Nullify();

        return Expression.New(ci, exp, rowId.UnNullify(), order);
    }

    internal Expression GetProjectorExpression(Alias tableAlias, QueryBinder binder, bool disableAssertAllowed = false)
    {
        if (!disableAssertAllowed)
            Schema.Current.AssertAllowed(this.BackReference.ReferenceTable.Type, inUserInterface: false);

        Type elementType = typeof(MListElement<,>).MakeGenericType(BackReference.FieldType, Field.FieldType);

        var rowId = RowIdExpression(tableAlias);

        IntervalExpression? period = GenerateSystemPeriod(tableAlias, binder);

        var backReference = (EntityExpression)this.BackReference.GetExpression(tableAlias, binder, null!, period, null);
        var entityContext = new EntityContextInfo(backReference.ExternalId, rowId);

        return new MListElementExpression(
            rowId,
            backReference,
            this.Order == null ? null : OrderExpression(tableAlias),
            this.Field.GetExpression(tableAlias, binder, rowId, period, entityContext),
            period,
            this,
            tableAlias);
    }

    internal IntervalExpression? GenerateSystemPeriod(Alias tableAlias, QueryBinder binder, bool force = false)
    {
        return this.SystemVersioned != null && (force || binder.systemTime is SystemTime.Interval) ?  this.SystemVersioned.IntervalExpression(tableAlias) : null;
    }

    ColumnExpression ITablePrivate.GetPrimaryOrder(Alias alias)
    {
        return (ColumnExpression)RowIdExpression(alias).Value;
    }
}

public abstract partial class Field
{
    internal abstract Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext);
}

public partial class FieldPrimaryKey
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        return new PrimaryKeyExpression(new ColumnExpression(this.Type.Nullify(), tableAlias, this.Name).Nullify());
    }
}

public partial class FieldValue
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
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

    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
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
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        Type cleanType = IsLite ? Lite.Extract(FieldType)! : FieldType;

        var result = new EntityExpression(cleanType, new PrimaryKeyExpression(new ColumnExpression(this.Type.Nullify(), tableAlias, Name)), period, null, null, null, null, AvoidExpandOnRetrieving);

        if (this.IsLite) {

            var customModelTypes = this.CustomLiteModelType == null ? null : new Dictionary<Type, Type> { { cleanType, this.CustomLiteModelType } };

            return QueryBinder.MakeLite(result, customModelTypes);
        }
        else
            return result;
    }
}

public partial class FieldEnum
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        return Expression.Convert(new ColumnExpression(this.Type, tableAlias, Name), FieldType);
    }
}

public partial class FieldMList
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        return new MListExpression(FieldType, (PrimaryKeyExpression)id, period, TableMList); // keep back id empty for some seconds
    }
}

public partial class FieldEmbedded
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        var bindings = (from kvp in EmbeddedFields
                        let fi = kvp.Value.FieldInfo
                        select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder, id, period, entityContext)))
                        .Concat(entityContext == null ? Enumerable.Empty<FieldBinding>() : Schema.Current.GetAdditionalQueryBindings(this.Route, entityContext, period))
                        .ToReadOnly();

        var mixins = this.Mixins?.Values.Select(m => (MixinEntityExpression)m.GetExpression(tableAlias, binder, id, period, entityContext)).ToReadOnly();

        Expression hasValue = HasValue == null ? SmartEqualizer.NotEqualNullable(id,
            id is PrimaryKeyExpression pk ? QueryBinder.NullId(pk.ValueType) : Expression.Constant(null, id.Type.Nullify())) :
            new ColumnExpression(((IColumn)HasValue).Type, tableAlias, HasValue.Name);

        return new EmbeddedEntityExpression(this.FieldType, hasValue, bindings, mixins, this, null, entityContext);
    }
}

public partial class FieldMixin
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        var bindings = (from kvp in Fields
                        let fi = kvp.Value.FieldInfo
                        select new FieldBinding(fi, kvp.Value.Field.GetExpression(tableAlias, binder, id, period, entityContext)))
                        .Concat(entityContext == null ? Enumerable.Empty<FieldBinding>() : Schema.Current.GetAdditionalQueryBindings(this.Route, entityContext, period))
                        .ToReadOnly();

        return new MixinEntityExpression(this.FieldType, bindings, tableAlias, this, entityContext);
    }
}

public partial class FieldImplementedBy
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        var implementations = ImplementationColumns.SelectDictionary(t => t, (t, ic) =>
             new EntityExpression(t, new PrimaryKeyExpression(new ColumnExpression(ic.Type.Nullify(), tableAlias, ic.Name)), period, null, null, null, null, AvoidExpandOnRetrieving));

        var result = new ImplementedByExpression(IsLite ? Lite.Extract(FieldType)! : FieldType, SplitStrategy, implementations);

        if (this.IsLite)
        {
            var customModelTypes = ImplementationColumns.Any(ic => ic.Value.CustomLiteModelType != null) ?
                ImplementationColumns.Where(a => a.Value.CustomLiteModelType != null).ToDictionaryEx(a => a.Key, a => a.Value.CustomLiteModelType!) : null;

            return QueryBinder.MakeLite(result, customModelTypes);
        }
        else
            return result;
    }
}

public partial class FieldImplementedByAll
{
    internal override Expression GetExpression(Alias tableAlias, QueryBinder binder, Expression id, IntervalExpression? period, EntityContextInfo? entityContext)
    {
        Expression result = new ImplementedByAllExpression(
            IsLite ? Lite.Extract(FieldType)! : FieldType,
            IdColumns.SelectDictionary(t => t, (t, col) => (Expression)new ColumnExpression(col.Type, tableAlias, col.Name)),
            new TypeImplementedByAllExpression(new PrimaryKeyExpression(new ColumnExpression(TypeColumn.Type.Nullify(), tableAlias, TypeColumn.Name))),
            period);

        if (this.IsLite)
            return QueryBinder.MakeLite(result, CustomLiteModelTypes);
        else
            return result;
    }
}
