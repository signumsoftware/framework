using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public class TypeDN : Entity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value); }
        }

        [NotNullable, UniqueIndex]
        string cleanName;
        public string CleanName
        {
            get { return cleanName; }
            set { Set(ref cleanName, value); }
        }

        [NotNullable, UniqueIndex]
        string tableName;
        public string TableName
        {
            get { return tableName; }
            set { Set(ref tableName, value); }
        }

        static Expression<Func<TypeDN, string>> ToStringExpression = e => e.CleanName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public bool IsType(Type type)
        {
            if (type == null)
                throw new ArgumentException("type");

            return FullClassName == type.FullName;
        }

        public string Namespace
        {
            get { return FullClassName.Substring(0, FullClassName.LastIndexOf('.').NotFound(0)); }
        }

        public string ClassName
        {
            get { return FullClassName.Substring(FullClassName.LastIndexOf('.') + 1); }
        }

        public static Func<Type, TypeDN> ToTypeDNFunc = t => { throw new InvalidOperationException("Lite.ToTypeDNFunc is not set"); };
        public static Func<TypeDN, Type> ToTypeFunc = t => { throw new InvalidOperationException("Lite.ToTypeFunc is not set"); };
        public static Func<string, Type> TryGetType = s => { throw new InvalidOperationException("Lite.TryGetType is not set"); };
        public static Func<Type, string> GetCleanName = s => { throw new InvalidOperationException("Lite.GetCleanName is not set"); };

        public static void SetTypeNameAndResolveType(Func<Type, string> getCleanName, Func<string, Type> tryGetType, Func<Type, TypeDN> toTypeDN, Func<TypeDN, Type> toType)
        {
            TypeDN.GetCleanName = getCleanName;
            TypeDN.TryGetType = tryGetType;
            TypeDN.ToTypeDNFunc = toTypeDN;
            TypeDN.ToTypeFunc = toType;
        }
    }

    public static class TypeDNExtensions
    {
        public static Type ToType(this TypeDN type)
        {
            return TypeDN.ToTypeFunc(type);
        }

        public static TypeDN ToTypeDN(this Type type)
        {
            return TypeDN.ToTypeDNFunc(type);
        }
    }
}
