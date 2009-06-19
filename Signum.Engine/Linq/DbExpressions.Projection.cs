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
    //internal class BaseReferenceExpression : Expression
    //{


    //    public BaseReferenceExpression(DbExpressionType expressionType, Type type, string alias, Expression id)
    //        : base((ExpressionType)expressionType, type)
    //    {
    //        this.ID = id;
    //        this.Alias = alias;
    //    }
    //}

    internal class FieldInitExpression : Expression
    {
        public readonly Expression ExternalId;
        public readonly string Alias; 

        // no es readonly!!!
        public ReadOnlyCollection<FieldBinding> Bindings;

        public FieldInitExpression(Type type, string alias, Expression externalId)
            : base((ExpressionType)DbExpressionType.FieldInit, type)
        {
            this.Alias = alias;
            this.ExternalId = externalId; 
        }

        public FieldInitExpression(Type type, ColumnExpression externalId)
            : base((ExpressionType)DbExpressionType.FieldInit, type)
        {
            this.Alias = externalId.Alias;
            this.ExternalId = externalId; 
        }

        public override string ToString()
        {
            return "new {0}({1}){2}".Formato(Type.TypeName(), ExternalId, Bindings.TryCC(b => "{{\r\n{0}\r\n}}".Formato(b.ToString(",\r\n ").Indent(4))) ?? "");
        }
    }

    internal static class FieldBindingExtensions
    {
        static FieldInfo idFi = ReflectionTools.GetFieldInfo<IdentifiableEntity>(ei => ei.id);
        static FieldInfo toStrFi = ReflectionTools.GetFieldInfo<IdentifiableEntity>(ie => ie.toStr); 

        public static ColumnExpression IDColumn(this ReadOnlyCollection<FieldBinding> bindings) 
        {
            return (ColumnExpression)bindings.Single(b => ReflectionTools.FieldEquals(b.FieldInfo, idFi)).Binding;
        }

        public static ColumnExpression ToStrColumn(this ReadOnlyCollection<FieldBinding> bindings)
        {
            return (ColumnExpression)bindings.Single(b => ReflectionTools.FieldEquals(b.FieldInfo, toStrFi)).Binding;
        }
    }

    public class FieldBinding
    {
        public readonly FieldInfo FieldInfo;
        public readonly Expression Binding;

        public FieldBinding(FieldInfo fieldInfo, Expression binding)
        {
            if (!fieldInfo.FieldType.IsAssignableFrom(binding.Type))
                throw new ApplicationException(Resources.TypeOfExpressionIs0ButTypeOfFieldIs1.Formato(binding.Type.TypeName(), fieldInfo.FieldType.TypeName()));
            this.FieldInfo = fieldInfo;
            this.Binding = binding;
        }

        public override string ToString()
        {
            return "{0} = {1}".Formato(FieldInfo.Name, Binding);
        }
    }

    internal class ImplementedByExpression : Expression
    {
        public readonly ReadOnlyCollection<ImplementationColumnExpression> Implementations;

        public ImplementedByExpression(Type type, ReadOnlyCollection<ImplementationColumnExpression> implementations)
            : base((ExpressionType)DbExpressionType.ImplementedBy, type)
        {
            this.Implementations = implementations.ToReadOnly();
        }

        public override string ToString()
        {
            return "ImplementedBy{{ {0} }}".Formato(Implementations.ToString(", "));
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
            return "Reference<{0}>({1})".Formato(Type.TypeName(), Field);
        }
    }

    internal class ImplementedByAllExpression: Expression
    {
        public List<ImplementationColumnExpression> Implementations = new List<ImplementationColumnExpression>();

        public readonly ColumnExpression ID;
        public readonly ColumnExpression TypeID;      

        public ImplementedByAllExpression(Type type, ColumnExpression id, ColumnExpression typeID)
            : base((ExpressionType)DbExpressionType.ImplementedByAll, type)
        {
            this.ID = id;
            this.TypeID = typeID;
        }

        public override string ToString()
        {
            return "ImplementedByAll{{ ID = {0}, Type = {1}}}".Formato(ID, TypeID);
        }
    }

    internal class LazyReferenceExpression: Expression
    {
        public readonly Expression Reference;

        public LazyReferenceExpression(Type type, Expression reference): 
            base((ExpressionType)DbExpressionType.LazyReference, type)
        {
            if (!Reflector.ExtractLazy(type).IsAssignableFrom(reference.Type))
                throw new ApplicationException(Resources.TheType0IsNotTheLazyVersionOf1.Formato(type.TypeName(), reference.Type.TypeName()));

            this.Reference = reference;
        }

        public override string ToString()
        {
            return "({0}).ToLazy()".Formato(Reference);
        }
    }

    internal class LazyLiteralExpression : Expression
    {
        public readonly Expression ID;
        public readonly Expression ToStr;
        public readonly Type RuntimeType;

        public LazyLiteralExpression(Type type, Type runtimeType, ColumnExpression id, ColumnExpression toStr) :
            base((ExpressionType)DbExpressionType.LazyLiteral, type)
        {
            if (!Reflector.ExtractLazy(type).IsAssignableFrom(runtimeType))
                throw new ApplicationException(Resources.TheType0IsNotTheLazyVersionOf1.Formato(type.TypeName(), runtimeType.TypeName()));


            Debug.Assert(id.Type.UnNullify() == typeof(int));
            Debug.Assert(toStr.Type == typeof(string));

            this.ID = id;
            this.ToStr = toStr; 
            this.RuntimeType = runtimeType;
        }


        public override string ToString()
        {
            return "new Lazy<{0}>({1}, {2})".Formato(Reflector.ExtractLazy(Type).Name, ID, ToStr);
        }
    }

    internal class MListExpression : Expression
    {
        public Expression BackID; // not readonly
        public readonly RelationalTable RelationalTable;

        public MListExpression(Type type, Expression backID, RelationalTable tr)
            :base((ExpressionType)DbExpressionType.MList, type)
        {
            this.BackID = backID;
            this.RelationalTable = tr;
        }
    }
}
