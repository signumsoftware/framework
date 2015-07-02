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

        public readonly Table Table;
        public readonly PrimaryKeyExpression ExternalId;

        //Optional
        public readonly Alias TableAlias;
        public readonly ReadOnlyCollection<FieldBinding> Bindings;
        public readonly ReadOnlyCollection<MixinEntityExpression> Mixins;

        public readonly bool AvoidExpandOnRetrieving;

        public EntityExpression(Type type, PrimaryKeyExpression externalId, Alias tableAlias, IEnumerable<FieldBinding> bindings, IEnumerable<MixinEntityExpression> mixins, bool avoidExpandOnRetrieving)
            : base(DbExpressionType.Entity, type)
        {
            if (type == null) 
                throw new ArgumentNullException("type");

            if (!type.IsEntity())
                throw new ArgumentException("type");
            
            if (externalId == null) 
                throw new ArgumentNullException("externalId");
            
            this.Table = Schema.Current.Table(type);
            this.ExternalId = externalId;

            this.TableAlias = tableAlias;
            this.Bindings = bindings.ToReadOnly();
            this.Mixins = mixins.ToReadOnly();

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
    }

  
    internal class EmbeddedEntityExpression : DbExpression
    {
        public readonly Expression HasValue; 

        public readonly ReadOnlyCollection<FieldBinding> Bindings;

        public readonly FieldEmbedded FieldEmbedded; //used for updates

        public EmbeddedEntityExpression(Type type, Expression hasValue, IEnumerable<FieldBinding> bindings, FieldEmbedded fieldEmbedded)
            : base(DbExpressionType.EmbeddedInit, type)
        {
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            if (hasValue == null || hasValue.Type != typeof(bool))
                throw new ArgumentException("hasValue should be a boolean expression");

            HasValue = hasValue;

            Bindings = bindings.ToReadOnly();

            FieldEmbedded = fieldEmbedded; 
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

        public ImplementedByAllExpression(Type type, Expression id, TypeImplementedByAllExpression typeId)
            : base(DbExpressionType.ImplementedByAll, type)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Type != typeof(string))
                throw new ArgumentException("string");

            if (typeId == null)
                throw new ArgumentNullException("typeId");

            this.Id = id;
            this.TypeId = typeId;
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
        public readonly Expression Reference; //Fie, ImplementedBy, ImplementedByAll or Constant to NullEntityExpression
        public readonly Expression CustomToStr; //Not readonly

        public LiteReferenceExpression(Type type, Expression reference, Expression customToStr) :
            base(DbExpressionType.LiteReference, type)
        {
            Type cleanType = Lite.Extract(type);

            if (cleanType != reference.Type)
                throw new ArgumentException("The type {0} is not the Lite version of {1}".FormatWith(type.TypeName(), reference.Type.TypeName()));

            this.Reference = reference;

            this.CustomToStr = customToStr;
        }

        public override string ToString()
        {
            return "({0}).ToLite({1})".FormatWith(Reference.ToString(), CustomToStr == null ? null : ("customToStr: " + CustomToStr.ToString()));
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitLiteReference(this);
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
            if (typeId == null)
                throw new ArgumentNullException("typeId");

            if (id == null)
                throw new ArgumentNullException("id");

            this.TypeId = typeId;
            this.Id = id;
            this.ToStr = toStr;
        }

        public override string ToString()
        {
            return "new Lite<{0}>({1},{2},{3})".FormatWith(Type.CleanType().TypeName(), TypeId.ToString(), Id.ToString(), ToStr.ToString());
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
            if (externalId == null)
                throw new ArgumentException("externalId");

            if (typeValue == null)
                throw new ArgumentException("typeValue"); 

            this.TypeValue = typeValue;
            this.ExternalId = externalId;
        }

        public override string ToString()
        {
            return "TypeFie({0};{1})".FormatWith(TypeValue.TypeName(), ExternalId.ToString());
        }

        protected override Expression Accept(DbExpressionVisitor visitor)
        {
            return visitor.VisitTypeFieldInit(this);
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
            if (typeColumn == null)
                throw new ArgumentException("typeId");

            this.TypeColumn = typeColumn;
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
        public readonly Expression BackID; // not readonly
        public readonly TableMList TableMList;

        public MListExpression(Type type, Expression backID, TableMList tr)
            :base(DbExpressionType.MList, type)
        {
            this.BackID = backID;
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

    internal class MListProjectionExpression : DbExpression
    {
        public readonly ProjectionExpression Projection;

        public MListProjectionExpression(Type type, ProjectionExpression projection)
            : base(DbExpressionType.MListProjection, type)
        {
            if (!projection.Type.ElementType().IsInstantiationOf(typeof(MList<>.RowIdValue)))
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

        public MListElementExpression(PrimaryKeyExpression rowId, EntityExpression parent, Expression order, Expression element, TableMList table)
            : base(DbExpressionType.MListElement, typeof(MListElement<,>).MakeGenericType(parent.Type, element.Type))
        {
            this.RowId = rowId;
            this.Parent = parent;
            this.Order = order;
            this.Element = element;
            this.Table = table;
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

            if(typeId == null)
                throw new ArgumentNullException("typeId");

            this.Id = id;
            this.TypeId = typeId;
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
