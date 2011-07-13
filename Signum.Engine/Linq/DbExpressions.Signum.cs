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
using Signum.Engine.Properties;
using System.Diagnostics;

namespace Signum.Engine.Linq
{
    internal class FieldInitExpression : DbExpression
    {
        public static readonly FieldInfo IdField = ReflectionTools.GetFieldInfo((IdentifiableEntity ei) =>ei.id);
        public static readonly FieldInfo ToStrField = ReflectionTools.GetFieldInfo((IdentifiableEntity ie) =>ie.toStr);

        public readonly Table Table;
        public readonly Expression ExternalId;
        public readonly Expression OtherCondition; //Used for IBA only, 
        public readonly ProjectionToken Token;


        public Alias TableAlias; //Changed on expansion 
        public List<FieldBinding> Bindings = new List<FieldBinding>();// not readonly!!!

        public FieldInitExpression(Type type, Alias tableAlias, Expression externalId, Expression otherCondition, ProjectionToken token)
            : base(DbExpressionType.FieldInit, type)
        {
            if (type == null) 
                throw new ArgumentNullException("type");

            if (!type.IsIIdentifiable())
                throw new ArgumentException("type");
            
            if (externalId == null) 
                throw new ArgumentNullException("externalId");
            
            this.Table = Schema.Current.Table(type);
            this.Token = token;
            this.TableAlias = tableAlias;
            this.ExternalId = externalId;
            this.OtherCondition = otherCondition;
        }

        public Expression GetOrCreateFieldBinding(FieldInfo fi, BinderTools tools)
        {
            FieldBinding binding = Bindings.SingleOrDefault(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo));
            if (binding != null)
                return binding.Binding;

            AssertTable(tools);

            Expression ex = Table.CreateBinding(Token, TableAlias, fi, tools);

            if (ex is MListExpression)
            {
                MListExpression mle = (MListExpression)ex;

                mle.BackID = GetOrCreateFieldBinding(FieldInitExpression.IdField, tools);
            }

            Bindings.Add(new FieldBinding(fi, ex));

            return ex; 
        }
        public void ReplaceBinding(FieldInfo fi, Expression expression)
        {
            Bindings.RemoveAll(a=>ReflectionTools.FieldEquals(a.FieldInfo, fi));
            Bindings.Add(new FieldBinding(fi, expression)); 
        }

        public Expression GetFieldBinding(FieldInfo fi)
        {
            FieldBinding binding = Bindings.Single(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo));

            return binding.Binding;
        }

        public override string ToString()
        {
            string constructor = "new {0}({1})".Formato(Type.TypeName(), ExternalId.NiceToString());
            string bindings = Bindings.TryCC(b => b.ToString(",\r\n ")) ?? "";

            return bindings.HasText() ?
                constructor + "\r\n{" + bindings.Indent(4) + "\r\n}" :
                constructor;
        }

        public void Complete(BinderTools tools)
        {
            AssertTable(tools);

            foreach (EntityField field in Table.Fields.Values.Where(f =>
                !ReflectionTools.Equals(f.FieldInfo, IdField)))
            {
                Expression exp = GetOrCreateFieldBinding(field.FieldInfo, tools);

                if (exp is MListExpression)
                {
                    Expression proj = tools.MListProjection((MListExpression)exp);
                    ReplaceBinding(field.FieldInfo, proj); 
                }  
            }
        }

        void AssertTable(BinderTools tools)
        {
            if (TableAlias == null)
            {
                TableAlias = tools.NextTableAlias(Table.Name);
                if (!Table.IsView)
                    GetOrCreateFieldBinding(FieldInitExpression.IdField, tools);
                tools.AddRequest(Token, new TableCondition
                {
                    FieldInit = this,
                    Table = new TableExpression(TableAlias, Table.Name)
                });
            }
        }
    }

  
    internal class EmbeddedFieldInitExpression : DbExpression
    {
        public readonly Expression HasValue; 

        public readonly ReadOnlyCollection<FieldBinding> Bindings;

        public readonly FieldEmbedded FieldEmbedded; //used for updates

        public EmbeddedFieldInitExpression(Type type, Expression hasValue, IEnumerable<FieldBinding> bindings, FieldEmbedded fieldEmbedded)
            : base(DbExpressionType.EmbeddedFieldInit, type)
        {
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            if (hasValue != null && hasValue.Type != typeof(bool))
                throw new ArgumentException("hasValue should be a boolean expression");

            HasValue = hasValue;

            Bindings = bindings.ToReadOnly();

            FieldEmbedded = fieldEmbedded; 
        }

        public Expression GetBinding(FieldInfo fi)
        {
            return Bindings.Single(fb => ReflectionTools.FieldEquals(fi, fb.FieldInfo)).Binding;
        }

        public override string ToString()
        {
            string constructor = "new {0}".Formato(Type.TypeName());

            string bindings = Bindings.TryCC(b => b.ToString(",\r\n ")) ?? "";

            return bindings.HasText() ? 
                constructor + "\r\n{" + bindings.Indent(4) + "\r\n}" : 
                constructor;
        }
    }

   

    internal class FieldBinding
    {
        public readonly FieldInfo FieldInfo;
        public readonly Expression Binding;

        public FieldBinding(FieldInfo fieldInfo, Expression binding)
        {
            if (!fieldInfo.FieldType.IsAssignableFrom(binding.Type))
                throw new ArgumentException("Type of expression is {0} but type of field is {1}".Formato(binding.Type.TypeName(), fieldInfo.FieldType.TypeName()));
            
            this.FieldInfo = fieldInfo;
            this.Binding = binding;
        }

        public override string ToString()
        {
            return "{0} = {1}".Formato(FieldInfo.Name, Binding.NiceToString());
        }
    }

    internal class PropertyBinding
    {
        public readonly PropertyInfo PropertyInfo;
        public readonly Expression Binding;

        public PropertyBinding(PropertyInfo propertyInfo, Expression binding)
        {
            if (!propertyInfo.PropertyType.UnNullify().IsAssignableFrom(binding.Type.UnNullify()))
                throw new ArgumentException("Type of expression is {0} but type of field is {1}".Formato(binding.Type.TypeName(), propertyInfo.PropertyType.TypeName()));

            //if (Reflector.FindFieldInfo(propertyInfo.DeclaringType, (PropertyInfo)propertyInfo, false) != null)
            //    throw new ArgumentException("{0} is a PropertyInfo, when a FieldInfo is available".Formato(propertyInfo.Name));

            this.PropertyInfo = propertyInfo;
            this.Binding = binding;
        }

        public override string ToString()
        {
            return "{0} = {1}".Formato(PropertyInfo.Name, Binding.NiceToString());
        }
    }

    internal class ImplementedByExpression : DbExpression//, IPropertyInitExpression
    {
        public readonly ReadOnlyCollection<ImplementationColumnExpression> Implementations;
        public List<PropertyBinding> PropertyBindings = new List<PropertyBinding>(); //For interface Access
  
        public ImplementedByExpression(Type type, ReadOnlyCollection<ImplementationColumnExpression> implementations)
            : base(DbExpressionType.ImplementedBy, type)
        {
            this.Implementations = implementations.ToReadOnly();
        }

        public Expression TryGetPropertyBinding(PropertyInfo pi)
        {
            PropertyBinding binding = PropertyBindings.SingleOrDefault(fb => ReflectionTools.PropertyEquals(pi, fb.PropertyInfo));

            if (binding == null) 
                return null;

            return binding.Binding;
        }

        public void AddPropertyBinding(PropertyInfo pi, Expression binding)
        {
            PropertyBindings.Add(new PropertyBinding(pi, binding));
        }

        public override string ToString()
        {
            string bindings = PropertyBindings.TryCC(b => b.ToString(",\r\n "));

            string bindings2 = bindings.HasText() ? "Bindings = {{\r\n{0}\r\n}}".Formato(bindings.Indent(4)) : null;
 
            return "ImplementedBy{{\r\n{0}\r\n}}".Formato(
                Implementations.ToString(",\r\n").Add(bindings2, ",\r\n").Indent(4)
                );
        }
    }

    internal class ImplementationColumnExpression
    {
        public readonly FieldInitExpression Field;
        public readonly Type Type;

        public ImplementationColumnExpression(Type type, FieldInitExpression field)
        {
            this.Type = type;
            this.Field = field;
        }

        public override string ToString()
        {
            return "{0} -> {1}".Formato(Type.TypeName(), Field.NiceToString());
        }
    }

    internal class ImplementedByAllExpression : DbExpression
    {
        public List<ImplementationColumnExpression> Implementations = new List<ImplementationColumnExpression>();

        public readonly Expression Id;
        public readonly TypeIdExpression TypeId;
        public readonly ProjectionToken Token;

        public ImplementedByAllExpression(Type type, Expression id, TypeIdExpression typeId, ProjectionToken token)
            : base(DbExpressionType.ImplementedByAll, type)
        {
            this.Id = id;
            this.TypeId = typeId;
            this.Token = token;
        }

        public override string ToString()
        {
            return "ImplementedByAll{{ ID = {0}, Type = {1}}}".Formato(Id, TypeId);
        }
    }

    internal class LiteReferenceExpression : DbExpression
    {
        public readonly Expression Reference; //Fie, ImplementedBy, ImplementedByAll or Constant to NullEntityExpression

        public readonly Expression Id;
        public readonly Expression ToStr;
        public readonly Expression TypeId;

        public LiteReferenceExpression(Type type, Expression reference, Expression id, Expression toStr, Expression typeId) :
            base(DbExpressionType.LiteReference, type)
        {
            if (reference != null)
            {
                Type cleanType = Reflector.ExtractLite(type);

                if (cleanType != reference.Type)
                    throw new ArgumentException("The type {0} is not the Lite version of {1}".Formato(type.TypeName(), reference.Type.TypeName()));
            }

            this.Reference = reference;
            this.Id = id;
            this.ToStr = toStr;
            this.TypeId = typeId;
        }

        public override string ToString()
        {
            return "({0}).ToLite({1},{2},{3})".Formato(Reference.NiceToString(), Id.NiceToString(), ToStr.NiceToString(), TypeId.NiceToString());
        }
    }

    internal class TypeIdExpression : DbExpression
    {
        public readonly Expression Column;

        public TypeIdExpression(Expression column)
            : base(DbExpressionType.TypeId, typeof(Type))
        {
            if (column == null || column.Type.UnNullify() != typeof(int))
                throw new ArgumentException("typeId");

            this.Column = column;
        }

        public override string ToString()
        {
            return "Schema.Current.IdToType({0})".Formato(Column.NiceToString());
        }
    }

    internal class MListExpression : DbExpression
    {
        public Expression BackID; // not readonly
        public readonly RelationalTable RelationalTable;

        public MListExpression(Type type, Expression backID, RelationalTable tr)
            :base(DbExpressionType.MList, type)
        {
            this.BackID = backID;
            this.RelationalTable = tr;
        }

        public override string ToString()
        {
            return "MList({0},{1})".Formato(RelationalTable.Name, BackID); 
        }
    }

    internal class MListElementExpression : DbExpression
    {
        public readonly Expression RowId;
        public readonly FieldInitExpression Parent;
        public readonly Expression Element;

        public readonly RelationalTable Table;

        public MListElementExpression(Expression rowId, FieldInitExpression parent, Expression element, RelationalTable table)
            : base(DbExpressionType.MListElement, typeof(MListElement<,>).MakeGenericType(parent.Type, element.Type))
        {
            this.RowId = rowId;
            this.Parent = parent;
            this.Element = element;
            this.Table = table;
        }

        public override string ToString()
        {
            return "MListElement({0})\r\n{{\r\nParent={1},\r\nElement={2}}})".Formato(RowId, Parent, Element);
        }
    }
}
