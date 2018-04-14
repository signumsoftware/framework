using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using System.Diagnostics;

namespace Signum.Engine.Linq
{
    internal class EntityExpression : DbExpression
    {
        public static readonly FieldInfo IdField = ReflectionTools.GetFieldInfo((Entity ei) =>ei.id);
        public static readonly FieldInfo ToStrField = ReflectionTools.GetFieldInfo((Entity ie) =>ie.toStr);
        public static readonly MethodInfo ToStringMethod = ReflectionTools.GetMethodInfo((object o) => o.ToString());
        public static readonly PropertyInfo IdOrNullProperty = ReflectionTools.GetPropertyInfo((Entity ei) => ei.IdOrNull);

        public readonly Table Table;
        public readonly PrimaryKeyExpression ExternalId;
        public readonly NewExpression ExternalPeriod;

        //Optional
        public readonly Alias TableAlias;
        public readonly ReadOnlyCollection<FieldBinding> Bindings;
        public readonly ReadOnlyCollection<MixinEntityExpression> Mixins;

        public readonly bool AvoidExpandOnRetrieving;

        public readonly NewExpression TablePeriod;


        public EntityExpression(Type type, PrimaryKeyExpression externalId, NewExpression externalPeriod, Alias tableAlias, IEnumerable<FieldBinding> bindings, IEnumerable<MixinEntityExpression> mixins, NewExpression tablePeriod, bool avoidExpandOnRetrieving)
            : base(DbExpressionType.Entity, type)
        {
            if (type == null) 
                throw new ArgumentNullException("type");

            if (!type.IsEntity())
                throw new ArgumentException("type");
            this.Table = Schema.Current.Table(type);
            this.ExternalId = externalId ?? throw new ArgumentNullException("externalId");

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
                (Bindings == null ? null : ("\r\n{\r\n " + Bindings.ToString(",\r\n ").Indent(4) + "\r\n}")) +
                (Mixins == null ? null : ("\r\n" + Mixins.ToString(m => ".Mixin({0})".FormatWith(m), "\r\n")));
        }

        public Expression GetBinding(FieldInfo fi)
        {
            if (Bindings == null)
                throw new InvalidOperationException("EntityInitiExpression not completed");

            FieldBinding binding = Bindings.Where(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).SingleEx(() => "field '{0}' in {1} (field Ignored?)".FormatWith(fi.Name, this.Type.TypeName()));
            
            return binding.Binding;
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

        public readonly FieldEmbedded FieldEmbedded; //used for updates
        public readonly Table ViewTable; //used for updates

        public EmbeddedEntityExpression(Type type, Expression hasValue, IEnumerable<FieldBinding> bindings, FieldEmbedded fieldEmbedded, Table viewTable)
            : base(DbExpressionType.EmbeddedInit, type)
        {
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            if (hasValue == null || hasValue.Type != typeof(bool))
                throw new ArgumentException("hasValue should be a boolean expression");

            HasValue = hasValue;

            Bindings = bindings.ToReadOnly();

            FieldEmbedded = fieldEmbedded; 
            ViewTable = viewTable;
        }

        public Expression GetBinding(FieldInfo fi)
        {
            return Bindings.SingleEx(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).Binding;
        }

        public override string ToString()
        {
            string constructor = "new {0}".FormatWith(Type.TypeName());

            string bindings = Bindings?.Let(b => b.ToString(",\r\n ")) ?? "";

            return bindings.HasText() ? 
                constructor + "\r\n{" + bindings.Indent(4) + "\r\n}" : 
                constructor;
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitEmbeddedEntity(this);
        }

        public Expression GetViewId()
        {
            var field = ViewTable.GetViewPrimaryKey();

            return this.Bindings.SingleEx(b => ReflectionTools.FieldEquals(b.FieldInfo, field.FieldInfo)).Binding;
        }
    }

    internal class MixinEntityExpression : DbExpression
    {
        public readonly ReadOnlyCollection<FieldBinding> Bindings;

        public readonly FieldMixin FieldMixin; //used for updates

        public readonly Alias MainEntityAlias;

        public MixinEntityExpression(Type type, IEnumerable<FieldBinding> bindings, Alias mainEntityAlias, FieldMixin fieldMixin)
            : base(DbExpressionType.MixinInit, type)
        {
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            Bindings = bindings.ToReadOnly();

            FieldMixin = fieldMixin;

            MainEntityAlias = mainEntityAlias;
        }

        public Expression GetBinding(FieldInfo fi)
        {
            return Bindings.SingleEx(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).Binding;
        }

        public override string ToString()
        {
            string constructor = "new {0}".FormatWith(Type.TypeName());

            string bindings = Bindings?.Let(b => b.ToString(",\r\n ")) ?? "";

            return bindings.HasText() ?
                constructor + "\r\n{" + bindings.Indent(4) + "\r\n}" :
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

            if (!ft.IsAssignableFrom(binding.Type))
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
            return "ImplementedBy({0}){{\r\n{1}\r\n}}".FormatWith(Strategy,
                Implementations.ToString(kvp => "{0} ->  {1}".FormatWith(kvp.Key.TypeName(), kvp.Value.ToString()), "\r\n").Indent(4)
                );
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitImplementedBy(this);
        }
    }

    internal class ImplementedByAllExpression : DbExpression
    {
        public readonly Expression Id;
        public readonly TypeImplementedByAllExpression TypeId;
        public readonly NewExpression ExternalPeriod;
        

        public ImplementedByAllExpression(Type type, Expression id, TypeImplementedByAllExpression typeId, NewExpression externalPeriod)
            : base(DbExpressionType.ImplementedByAll, type)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Type != typeof(string))
                throw new ArgumentException("string");
            this.Id = id;
            this.TypeId = typeId ?? throw new ArgumentNullException("typeId");
            this.ExternalPeriod = externalPeriod;
        }

        public override string ToString()
        {
            return "ImplementedByAll{{ ID = {0}, Type = {1} }}".FormatWith(Id, TypeId);
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitImplementedByAll(this);
        }
    }

    internal class LiteReferenceExpression : DbExpression
    {
        public bool LazyToStr;
        public bool EagerEntity;
        public readonly Expression Reference; //Fie, ImplementedBy, ImplementedByAll or Constant to NullEntityExpression
        public readonly Expression CustomToStr; //Not readonly

        public LiteReferenceExpression(Type type, Expression reference, Expression customToStr, bool lazyToStr, bool eagerEntity) :
            base(DbExpressionType.LiteReference, type)
        {
            Type cleanType = Lite.Extract(type);

            if (cleanType != reference.Type)
                throw new ArgumentException("The type {0} is not the Lite version of {1}".FormatWith(type.TypeName(), reference.Type.TypeName()));

            this.Reference = reference;

            this.CustomToStr = customToStr;

            this.LazyToStr = lazyToStr;
            this.EagerEntity = eagerEntity;
        }

        public override string ToString()
        {
            return "({0}).ToLite({1})".FormatWith(Reference.ToString(), CustomToStr == null ? null : ("customToStr: " + CustomToStr.ToString()));
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
                    return new LiteReferenceExpression(this.Type, this.Reference, this.CustomToStr, lazyToStr: false, eagerEntity: true);
                case ExpandLite.ToStringEager:
                    return new LiteReferenceExpression(this.Type, this.Reference, this.CustomToStr, lazyToStr: false, eagerEntity: false);
                case ExpandLite.ToStringLazy:
                    return new LiteReferenceExpression(this.Type, this.Reference, this.CustomToStr, lazyToStr: true, eagerEntity: false);
                case ExpandLite.ToStringNull:
                    return new LiteReferenceExpression(this.Type, this.Reference, Expression.Constant(null, typeof(string)), lazyToStr: true, eagerEntity: false);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal class LiteValueExpression : DbExpression
    {
        public readonly Expression TypeId;
        public readonly Expression Id;
        public readonly Expression ToStr; //Not readonly


        public LiteValueExpression(Type type, Expression typeId, Expression id, Expression toStr) :
            base(DbExpressionType.LiteValue, type)
        {
            this.TypeId = typeId ?? throw new ArgumentNullException("typeId");
            this.Id = id ?? throw new ArgumentNullException("id");
            this.ToStr = toStr;
        }

        public override string ToString()
        {
            return $"new Lite<{Type.CleanType().TypeName()}>({TypeId},{Id},{ToStr})";
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitLiteValue(this);
        }
    }

    internal class TypeEntityExpression : DbExpression
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
            return "TypeFie({0};{1})".FormatWith(TypeValue.TypeName(), ExternalId.ToString());
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitTypeEntity(this);
        }
    }

    internal class TypeImplementedByExpression : DbExpression
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

    internal class TypeImplementedByAllExpression : DbExpression
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
        public readonly PrimaryKeyExpression BackID; // not readonly
        public readonly TableMList TableMList;
        public readonly NewExpression ExternalPeriod;

        public MListExpression(Type type, PrimaryKeyExpression backID, NewExpression externalPeriod, TableMList tr)
            : base(DbExpressionType.MList, type)
        {
            this.BackID = backID;
            this.ExternalPeriod = externalPeriod;
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
        public readonly NewExpression ExternalPeriod;
        public readonly PropertyRoute Route;

        public AdditionalFieldExpression(Type type, PrimaryKeyExpression backID, NewExpression externalPeriod, PropertyRoute route)
            : base(DbExpressionType.AdditionalField, type)
        {
            this.BackID = backID;
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
            if (!projection.Type.ElementType().IsInstantiationOf(typeof(MList<>.RowIdElement)))
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
        public readonly Expression Order;
        public readonly Expression Element;

        public readonly TableMList Table;

        public readonly Alias Alias;

        public readonly NewExpression TablePeriod;

        public MListElementExpression(PrimaryKeyExpression rowId, EntityExpression parent, Expression order, Expression element, NewExpression systemPeriod, TableMList table, Alias alias)
            : base(DbExpressionType.MListElement, typeof(MListElement<,>).MakeGenericType(parent.Type, element.Type))
        {
            this.RowId = rowId;
            this.Parent = parent;
            this.Order = order;
            this.Element = element;
            this.TablePeriod = systemPeriod;
            this.Table = table;
            this.Alias = alias;
        }

        public override string ToString()
        {
            return "MListElement({0})\r\n{{\r\nParent={1},\r\nOrder={2},\r\nElement={3}}})".FormatWith(
                RowId.ToString(), 
                Parent.ToString(), 
                Order == null ? Order.ToString() : null, 
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

    internal class PrimaryKeyStringExpression : DbExpression
    {
        public readonly Expression Id;
        public readonly TypeImplementedByAllExpression TypeId;

        public PrimaryKeyStringExpression(Expression id, TypeImplementedByAllExpression typeId)
            : base(DbExpressionType.PrimaryKeyString, typeof(PrimaryKey?))
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if(id.Type != typeof(string))
                throw new ArgumentException("id should be a string");

            this.Id = id;
            this.TypeId = typeId ?? throw new ArgumentNullException("typeId");
        }

        public override string ToString()
        {
            return "(PrimaryKeyString?)(" + Id.ToString() + ", " + TypeId.ToString() + ")";
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitPrimaryKeyString(this);
        }
    }
}
