using System.Collections.ObjectModel;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;

namespace Signum.Engine.Linq;

internal class EntityExpression : DbExpression
{
    public static readonly FieldInfo IdField = ReflectionTools.GetFieldInfo((Entity ei) =>ei.id);
    public static readonly FieldInfo ToStrField = ReflectionTools.GetFieldInfo((Entity ie) =>ie.ToStr);
    public static readonly MethodInfo ToStringMethod = ReflectionTools.GetMethodInfo((object o) => o.ToString());
    public static readonly PropertyInfo IdOrNullProperty = ReflectionTools.GetPropertyInfo((Entity ei) => ei.IdOrNull);

    public readonly Table Table;
    public readonly PrimaryKeyExpression ExternalId;
    public readonly IntervalExpression? ExternalPeriod;

    //Optional
    public readonly Alias? TableAlias;
    public readonly ReadOnlyCollection<FieldBinding>? Bindings;
    public readonly ReadOnlyCollection<MixinEntityExpression>? Mixins;

    public readonly bool AvoidExpandOnRetrieving;

    public readonly IntervalExpression? TablePeriod;


    public EntityExpression(Type type, PrimaryKeyExpression externalId, 
        IntervalExpression? externalPeriod, 
        Alias? tableAlias, 
        IEnumerable<FieldBinding>? bindings, 
        IEnumerable<MixinEntityExpression>? mixins,
        IntervalExpression? tablePeriod, bool avoidExpandOnRetrieving)
        : base(DbExpressionType.Entity, type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsEntity())
            throw new ArgumentException("type");
        this.Table = Schema.Current.Table(type);
        this.ExternalId = externalId ?? throw new ArgumentNullException(nameof(externalId));

        this.TableAlias = tableAlias;
        this.Bindings = bindings.ToReadOnly();
        this.Mixins = mixins.ToReadOnly();

        this.ExternalPeriod = externalPeriod;
        this.TablePeriod = tablePeriod;

        this.AvoidExpandOnRetrieving = avoidExpandOnRetrieving;
    }

    public override string ToString()
    {
        var constructor = "new {0}{1}({2})".FormatWith(Type.TypeName(), AvoidExpandOnRetrieving ? "?": "",
            ExternalId.ToString());

        return constructor +
            (Bindings == null ? null : ("\n{\n " + Bindings.ToString(",\n ").Indent(4) + "\n}")) +
            (Mixins == null ? null : ("\n" + Mixins.ToString(m => ".Mixin({0})".FormatWith(m), "\n")));
    }

    public Expression GetBinding(FieldInfo fi)
    {
        if (Bindings == null)
            throw new InvalidOperationException("EntityInitiExpression not completed");

        FieldBinding binding = Bindings.Where(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).SingleEx(() => "field '{0}' in {1} (field Ignored?)".FormatWith(fi.Name, this.Type.TypeName()));

        return binding.Binding;
    }
 
    public Expression? GetPartitionId()
    {
        FieldBinding? binding = Bindings?.SingleOrDefaultEx(fb => ReflectionTools.FieldEquals(Signum.Engine.Maps.Table.fiPartitionId, fb.FieldInfo));
        return binding?.Binding;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitEntity(this);
    }

    internal EntityExpression WithExpandEntity(ExpandEntity expandEntity)
    {
        switch (expandEntity)
        {
            case ExpandEntity.EagerEntity:
                return new EntityExpression(this.Type, this.ExternalId, this.ExternalPeriod, this.TableAlias, this.Bindings, this.Mixins, this.TablePeriod, avoidExpandOnRetrieving: false);
            case ExpandEntity.LazyEntity:
                return new EntityExpression(this.Type, this.ExternalId, this.ExternalPeriod, this.TableAlias, this.Bindings, this.Mixins, this.TablePeriod, avoidExpandOnRetrieving: true);
            default:
                throw new NotImplementedException();
        }
    }
}


internal class EmbeddedEntityExpression : DbExpression
{
    public readonly Expression HasValue;

    public readonly ReadOnlyCollection<FieldBinding> Bindings;
    public readonly ReadOnlyCollection<MixinEntityExpression>? Mixins;
    public readonly EntityContextInfo? EntityContext; 

    public readonly FieldEmbedded? FieldEmbedded; //used for updates
    public readonly Table? ViewTable; //used for updates

    public EmbeddedEntityExpression(Type type, Expression hasValue, IEnumerable<FieldBinding> bindings,
        IEnumerable<MixinEntityExpression>? mixins, FieldEmbedded? fieldEmbedded, Table? viewTable, EntityContextInfo? entityContext)
        : base(DbExpressionType.EmbeddedInit, type)
    {
        if (bindings == null)
            throw new ArgumentNullException(nameof(bindings));

        if (hasValue == null || hasValue.Type != typeof(bool))
            throw new ArgumentException("hasValue should be a boolean expression");

        HasValue = hasValue;

        Bindings = bindings.ToReadOnly();
        Mixins = mixins.ToReadOnly();

        FieldEmbedded = fieldEmbedded;
        EntityContext = entityContext;
        ViewTable = viewTable;
    }

    public Expression GetBinding(FieldInfo fi)
    {
        return Bindings.SingleEx(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).Binding;
    }

    public override string ToString()
    {
        string constructor = "new {0}".FormatWith(Type.TypeName());

        string bindings = Bindings?.Let(b => b.ToString(",\n ")) ?? "";

        return bindings.HasText() ?
            constructor + "\n{" + bindings.Indent(4) + "\n}" :
            constructor;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitEmbeddedEntity(this);
    }

    public Expression GetViewId()
    {
        var field = ViewTable!.GetViewPrimaryKey()!;

        return this.Bindings.SingleEx(b => ReflectionTools.FieldEquals(b.FieldInfo, field.FieldInfo)).Binding;
    }
}

internal class EntityContextInfo
{
    public readonly PrimaryKeyExpression EntityId;
    public readonly Expression? EntityPartitionId;
    public readonly PrimaryKeyExpression? MListRowId;

    public EntityContextInfo(PrimaryKeyExpression entityId, Expression? entityPartitionId, PrimaryKeyExpression? mlistRowId)
    {
        EntityId = entityId;
        EntityPartitionId = entityPartitionId;
        MListRowId = mlistRowId;
    }
}

internal class MixinEntityExpression : DbExpression
{
    public readonly ReadOnlyCollection<FieldBinding> Bindings;

    public readonly EntityContextInfo? EntityContext;

    public readonly FieldMixin? FieldMixin; //used for updates

    public readonly Alias? MainEntityAlias;

    public MixinEntityExpression(Type type, IEnumerable<FieldBinding> bindings, Alias? mainEntityAlias, FieldMixin? fieldMixin, EntityContextInfo? info)
        : base(DbExpressionType.MixinInit, type)
    {
        if (bindings == null)
            throw new ArgumentNullException(nameof(bindings));

        Bindings = bindings.ToReadOnly();

        FieldMixin = fieldMixin;

        EntityContext = info;

        MainEntityAlias = mainEntityAlias;
    }

    public Expression GetBinding(FieldInfo fi)
    {
        return Bindings.SingleEx(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).Binding;
    }

    public override string ToString()
    {
        string constructor = "new {0}".FormatWith(Type.TypeName());

        string bindings = Bindings?.Let(b => b.ToString(",\n ")) ?? "";

        return bindings.HasText() ?
            constructor + "\n{" + bindings.Indent(4) + "\n}" :
            constructor;
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitMixinEntity(this);
    }
}



internal class FieldBinding
{
    public readonly FieldInfo FieldInfo;
    public readonly Expression Binding;

    public FieldBinding(FieldInfo fieldInfo, Expression binding, bool allowForcedNull = false)
    {
        var ft = fieldInfo.FieldType;
        if(allowForcedNull)
            ft = ft.Nullify();

        if (!ft.IsAssignableFrom(binding.Type) && !(!ft.IsValueType && binding.IsNull()))
            throw new ArgumentException("Type of expression is {0} but type of field is {1}".FormatWith(binding.Type.TypeName(), fieldInfo.FieldType.TypeName()));

        this.FieldInfo = fieldInfo;
        this.Binding = binding;
    }

    public override string ToString()
    {
        return "{0} = {1}".FormatWith(FieldInfo.Name, Binding.ToString());
    }
}

internal class ImplementedByExpression : DbExpression//, IPropertyInitExpression
{
    public readonly ReadOnlyDictionary<Type, EntityExpression> Implementations;

    public readonly CombineStrategy Strategy;

    public ImplementedByExpression(Type type, CombineStrategy strategy, IDictionary<Type, EntityExpression> implementations)
        : base(DbExpressionType.ImplementedBy, type)
    {
        this.Implementations = implementations.ToReadOnly();
        this.Strategy = strategy;
    }

    public override string ToString()
    {
        return "ImplementedBy({0}){{\n{1}\n}}".FormatWith(Strategy,
            Implementations.ToString(kvp => "{0} ->  {1}".FormatWith(kvp.Key.TypeName(), kvp.Value.ToString()), "\n").Indent(4)
            );
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitImplementedBy(this);
    }
}

internal class ImplementedByAllExpression : DbExpression
{
    public readonly ReadOnlyDictionary<Type/*PrimaryKey type*/, Expression> Ids;
    public readonly TypeImplementedByAllExpression TypeId;
    public readonly IntervalExpression? ExternalPeriod;


    public ImplementedByAllExpression(Type type, IDictionary<Type/*PrimaryKey type*/, Expression> ids, TypeImplementedByAllExpression typeId, IntervalExpression? externalPeriod)
        : base(DbExpressionType.ImplementedByAll, type)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));
        
        this.Ids = ids.ToReadOnly();
        this.TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
        this.ExternalPeriod = externalPeriod;
    }

    public override string ToString()
    {
        return "ImplementedByAll{{\n  Ids = {0},\n  Type = {1}\n}}".FormatWith(
            Ids.ToString(kvp => "{0} ->  {1}".FormatWith(kvp.Key.TypeName(), kvp.Value.ToString()), "\n"), 
            TypeId);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitImplementedByAll(this);
    }
}

internal class LiteReferenceExpression : DbExpression
{
    public bool LazyModel;
    public bool EagerEntity;
    public readonly Expression Reference; //Fie, ImplementedBy, ImplementedByAll or Constant to NullEntityExpression
    public readonly Expression? CustomModelExpression;
    public readonly ReadOnlyDictionary<Type, Type>? CustomModelTypes; 

    public LiteReferenceExpression(Type type, Expression reference, Expression? customModelExpression, ReadOnlyDictionary<Type, Type>? customModelTypes, bool lazyModel, bool eagerEntity) :
        base(DbExpressionType.LiteReference, type)
    {
        Type? cleanType = Lite.Extract(type);

        if (cleanType != reference.Type)
            throw new ArgumentException("The type {0} is not the Lite version of {1}".FormatWith(type.TypeName(), reference.Type.TypeName()));

        if (customModelExpression != null && customModelTypes != null)
            throw new InvalidOperationException($"{nameof(customModelExpression)} and {nameof(customModelTypes)} are incompatible");

        this.Reference = reference;

        this.CustomModelExpression = customModelExpression;
        this.CustomModelTypes = customModelTypes;

        this.LazyModel = lazyModel;
        this.EagerEntity = eagerEntity;
    }

    public override string ToString()
    {
        return "({0}).ToLite({1})".FormatWith(Reference.ToString(),
            CustomModelExpression != null ? ("custmoModelExpression: " + CustomModelExpression.ToString()) :
            CustomModelTypes != null ? ("custmoModelTypes: {\n" + CustomModelTypes.ToString(kvp => kvp.Key.TypeName() + ": " + kvp.Value.TypeName(), "\n, ").Indent(4) + "\n}") :
            null);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitLiteReference(this);
    }

    internal LiteReferenceExpression WithExpandLite(ExpandLite expandLite)
    {
        switch (expandLite)
        {
            case ExpandLite.EntityEager:
                return new LiteReferenceExpression(this.Type, this.Reference, this.CustomModelExpression, this.CustomModelTypes, lazyModel: false, eagerEntity: true);
            case ExpandLite.ModelEager:
                return new LiteReferenceExpression(this.Type, this.Reference, this.CustomModelExpression, this.CustomModelTypes, lazyModel: false, eagerEntity: false);
            case ExpandLite.ModelLazy:
                return new LiteReferenceExpression(this.Type, this.Reference, this.CustomModelExpression, this.CustomModelTypes, lazyModel: true, eagerEntity: false);
            case ExpandLite.ModelNull:
                return new LiteReferenceExpression(this.Type, this.Reference, Expression.Constant(null, typeof(string)), null, lazyModel: true, eagerEntity: false);
            default:
                throw new NotImplementedException();
        }
    }
}

public struct ExpressionOrType
{
    public readonly Expression? EagerExpression;
    public readonly Type? LazyModelType;

    public ExpressionOrType(Type lazyModelType)
    {
        LazyModelType = lazyModelType;
        EagerExpression = null;
    }

    public ExpressionOrType(Expression eagerExpression)
    {
        LazyModelType = null;
        EagerExpression = eagerExpression;
    }

    public override string ToString()
    {
        return LazyModelType?.TypeName() ?? EagerExpression!.ToString();
    }
}

internal class LiteValueExpression : DbExpression
{
    public readonly Expression TypeId;
    public readonly PrimaryKeyExpression Id;
    public readonly Expression? CustomModelExpression; 
    public readonly ReadOnlyDictionary<Type, ExpressionOrType>? Models;
    public readonly ReadOnlyDictionary<Type, Expression>? PartitionIds;


    public LiteValueExpression(Type type, Expression typeId, PrimaryKeyExpression id, Expression? customModelExpression, ReadOnlyDictionary<Type, ExpressionOrType>? models, ReadOnlyDictionary<Type, Expression>? partitionIds) :
        base(DbExpressionType.LiteValue, type)
    {
        this.TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
        this.Id = id ?? throw new ArgumentNullException(nameof(id));

        if (customModelExpression != null && models != null)
            throw new InvalidOperationException($"{nameof(customModelExpression)} and {models} are incomatible");

        this.CustomModelExpression = customModelExpression;
        this.Models = models;
        this.PartitionIds = partitionIds;
    }

    public override string ToString()
    {
        var lastPart = CustomModelExpression != null ? ("custmoModelExpression: " + CustomModelExpression.ToString()) :
            Models != null ? ("models: {\n" + Models.ToString(kvp => kvp.Key.TypeName() + ": " + kvp.Value.ToString(), "\n, ").Indent(4) + "\n}") :
            PartitionIds != null ? ("partitions: {\n" + PartitionIds.ToString(kvp => kvp.Key.TypeName() + ": " + kvp.Value.ToString(), "\n, ").Indent(4) + "\n}") :
            null;

        return $"new Lite<{Type.CleanType().TypeName()}>({TypeId},{Id}, {lastPart})";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitLiteValue(this);
    }
}

internal abstract class TypeDbExpression : DbExpression
{
    public TypeDbExpression(DbExpressionType dbType, Type type)
       : base(dbType, type)
    {
    }
}

internal class TypeEntityExpression : TypeDbExpression
{
    public readonly PrimaryKeyExpression ExternalId;
    public readonly Type TypeValue;

    public TypeEntityExpression(PrimaryKeyExpression externalId, Type typeValue)
        : base(DbExpressionType.TypeEntity, typeof(Type))
    {
        this.TypeValue = typeValue ?? throw new ArgumentException("typeValue");
        this.ExternalId = externalId ?? throw new ArgumentException("externalId");
    }

    public override string ToString()
    {
        return "TypeEntity({0};{1})".FormatWith(TypeValue.TypeName(), ExternalId.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitTypeEntity(this);
    }
}

internal class TypeImplementedByExpression : TypeDbExpression
{
    public readonly ReadOnlyDictionary<Type, PrimaryKeyExpression> TypeImplementations;

    public TypeImplementedByExpression(IDictionary<Type, PrimaryKeyExpression> typeImplementations)
        : base(DbExpressionType.TypeImplementedBy, typeof(Type))
    {
        if (typeImplementations == null || typeImplementations.Any(a => a.Value.Type.UnNullify() != typeof(PrimaryKey)))
            throw new ArgumentException("typeId");

        this.TypeImplementations = typeImplementations.ToReadOnly();
    }

    public override string ToString()
    {
        return "TypeIb({0})".FormatWith(TypeImplementations.ToString(kvp => "{0}({1})".FormatWith(kvp.Key.TypeName(), kvp.Value.ToString()), " | "));
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitTypeImplementedBy(this);
    }
}


internal class TypeImplementedByAllExpression : TypeDbExpression
{
    public readonly PrimaryKeyExpression TypeColumn;

    public TypeImplementedByAllExpression(PrimaryKeyExpression typeColumn)
        : base(DbExpressionType.TypeImplementedByAll, typeof(Type))
    {
        this.TypeColumn = typeColumn ?? throw new ArgumentException("typeId");
    }

    public override string ToString()
    {
        return "TypeIba({0})".FormatWith(TypeColumn.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitTypeImplementedByAll(this);
    }
}

internal class MListExpression : DbExpression
{
    public readonly PrimaryKeyExpression BackID;
    public readonly TableMList TableMList;
    public readonly IntervalExpression? ExternalPeriod;
    public readonly Expression? ExternalPartitionId;

    public MListExpression(Type type, PrimaryKeyExpression backID, IntervalExpression? externalPeriod, Expression? externalPartitionId, TableMList tr)
        : base(DbExpressionType.MList, type)
    {
        this.BackID = backID;
        this.ExternalPeriod = externalPeriod;
        this.ExternalPartitionId = externalPartitionId;
        this.TableMList = tr;
    }

    public override string ToString()
    {
        return "new MList({0},{1})".FormatWith(TableMList.Name, BackID);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitMList(this);
    }
}

internal class AdditionalFieldExpression : DbExpression
{
    public readonly PrimaryKeyExpression BackID; // not readonly
    public readonly PrimaryKeyExpression? MListRowId; // not readonly
    public readonly IntervalExpression? ExternalPeriod;
    public readonly PropertyRoute Route;

    public AdditionalFieldExpression(Type type, PrimaryKeyExpression backID, PrimaryKeyExpression? mlistRowId, IntervalExpression? externalPeriod, PropertyRoute route)
        : base(DbExpressionType.AdditionalField, type)
    {
        this.BackID = backID;
        this.MListRowId = mlistRowId;
        this.Route = route;
        this.ExternalPeriod = externalPeriod;
    }

    public override string ToString()
    {
        return "new AdditionalField({0})".FormatWith(this.Route);
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitAdditionalField(this);
    }
}

internal class MListProjectionExpression : DbExpression
{
    public readonly ProjectionExpression Projection;

    public MListProjectionExpression(Type type, ProjectionExpression projection)
        : base(DbExpressionType.MListProjection, type)
    {
        if (!projection.Type.ElementType()!.IsInstantiationOf(typeof(MList<>.RowIdElement)))
            throw new ArgumentException("projector should be collation of RowIdValue");

        this.Projection = projection;
    }

    public override string ToString()
    {
        return "new MList({0})".FormatWith(Projection.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitMListProjection(this);
    }
}

internal class MListElementExpression : DbExpression
{
    public readonly PrimaryKeyExpression RowId;
    public readonly EntityExpression Parent;
    public readonly Expression? Order;
    public readonly Expression? PartitionId;
    public readonly Expression Element;

    public readonly TableMList Table;

    public readonly Alias Alias;

    public readonly IntervalExpression? TablePeriod;

    public MListElementExpression(PrimaryKeyExpression rowId, EntityExpression parent, Expression? order, Expression? partitionId, Expression element, IntervalExpression? systemPeriod, TableMList table, Alias alias)
        : base(DbExpressionType.MListElement, typeof(MListElement<,>).MakeGenericType(parent.Type, element.Type))
    {
        this.RowId = rowId;
        this.Parent = parent;
        this.PartitionId = partitionId;
        this.Order = order;
        this.Element = element;
        this.TablePeriod = systemPeriod;
        this.Table = table;
        this.Alias = alias;
    }

    public override string ToString()
    {
        return "MListElement({0})\n{{\nParent={1},\nOrder={2},\nPartitionId={3}\nElement={4}}})".FormatWith(
            RowId.ToString(),
            Parent.ToString(),
            Order?.ToString(),
            PartitionId?.ToString(),
            Element.ToString());
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitMListElement(this);
    }
}

internal class PrimaryKeyExpression : DbExpression
{
    public static Variable<bool> PreferVariableNameVariable = Statics.ThreadVariable<bool>("preferParameterName");

    public static IDisposable PreferVariableName()
    {
        var oldValue = PreferVariableNameVariable.Value;
        PreferVariableNameVariable.Value = true;
        return new Disposable(() => PreferVariableNameVariable.Value = oldValue);
    }

    public readonly Expression Value;

    public Type ValueType { get { return Value.Type; } }

    public PrimaryKeyExpression(Expression value)
        : base(DbExpressionType.PrimaryKey, typeof(PrimaryKey?))
    {
        if (value.Type.Nullify() != value.Type)
            throw new InvalidOperationException("value should be nullable");

        this.Value = value;
    }

    public override string ToString()
    {
        return "(PrimaryKey?)(" + Value.ToString() + ")";
    }

    protected override Expression Accept(DbExpressionVisitor visitor)
    {
        return visitor.VisitPrimaryKey(this);
    }
}

