using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public class TypeEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Max = 200), UniqueIndex]
        public string TableName { get; set; }

        [StringLengthValidator(AllowNulls = false, Max = 200), UniqueIndex]
        public string CleanName { get; set; }

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string Namespace { get; set; }

        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string ClassName { get; set; }
        
        static Expression<Func<TypeEntity, string>> FullClassNameExpression =
            t => t.Namespace + "." + t.ClassName;
        [ExpressionField]
        public string FullClassName
        {
            get { return FullClassNameExpression.Evaluate(this); }
        }

        static Expression<Func<TypeEntity, string>> ToStringExpression = e => e.CleanName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public bool IsType(Type type)
        {
            if (type == null)
                throw new ArgumentException("type");

            return ClassName == type.Name && Namespace == type.Namespace;
        }

        public static Func<Type, TypeEntity> ToTypeEntityFunc = t => { throw new InvalidOperationException("Lite.ToTypeDNFunc is not set"); };
        public static Func<TypeEntity, Type> ToTypeFunc = t => { throw new InvalidOperationException("Lite.ToTypeFunc is not set"); };
        public static Func<string, Type> TryGetType = s => { throw new InvalidOperationException("Lite.TryGetType is not set"); };
        public static Func<Type, string> GetCleanName = s => { throw new InvalidOperationException("Lite.GetCleanName is not set"); };

        public static bool AlreadySet { get; private set; }
        public static void SetTypeNameCallbacks(Func<Type, string> getCleanName, Func<string, Type> tryGetType)
        {
            TypeEntity.GetCleanName = getCleanName;
            TypeEntity.TryGetType = tryGetType;

            AlreadySet = true;
        }

        public static void SetTypeEntityCallbacks(Func<Type, TypeEntity> toTypeEntity, Func<TypeEntity, Type> toType)
        {
            TypeEntity.ToTypeEntityFunc = toTypeEntity;
            TypeEntity.ToTypeFunc = toType;
        }
    }

    public static class TypeEntityExtensions
    {
        public static Type ToType(this TypeEntity type)
        {
            return TypeEntity.ToTypeFunc(type);
        }

        public static TypeEntity ToTypeEntity(this Type type)
        {
            return TypeEntity.ToTypeEntityFunc(type);
        }
    }
}
