using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Engine;
using System.Reflection;
using Signum.Entities.Basics;

namespace Signum.Web
{
    public class TypeContextUtilities
    {
        public static IEnumerable<TypeElementContext<S>> TypeElementContext<S>(TypeContext<MList<S>> typeContext)
        {
            var innerList = ((IMListPrivate<S>)typeContext.Value).InnerList;

            for (int i = 0; i < innerList.Count; i++)
            {
                var econtext = new TypeElementContext<S>(innerList[i].Element, typeContext, i, innerList[i].RowId);
                yield return econtext;
            }
        }

        static MethodInfo miRetrieve = ReflectionTools.GetMethodInfo((Lite<TypeEntity> l) => l.Retrieve()).GetGenericMethodDefinition();
        public static TypeContext CleanTypeContext(TypeContext typeContext)
        {
            if (typeContext == null)
                throw new ArgumentNullException("typeContext");

            if (typeContext.UntypedValue == null || 
                (!typeContext.Type.IsLite() && typeContext.Type == typeContext.UntypedValue.GetType()))
                return typeContext;

            ParameterExpression pe = Expression.Parameter(typeContext.Type, "p");

            Expression body = null;

            if (typeContext.Type.IsLite())
            {
                Lite<IEntity> lite = typeContext.UntypedValue as Lite<IEntity>;
                Type liteType = Lite.Extract(typeContext.Type);

                body = Expression.Call(miRetrieve.MakeGenericMethod(liteType), pe);
                if (lite.EntityType != liteType)
                    body = Expression.Convert(body, lite.EntityType);
            }
            else
            {
                body = Expression.Convert(pe, typeContext.UntypedValue.GetType());
            }

            LambdaExpression lambda = Expression.Lambda(body, pe);
            return Common.UntypedWalkExpression(typeContext, lambda);
        }

        public static TypeContext UntypedNew(IRootEntity entity, string prefix)
        {
            return (TypeContext)Activator.CreateInstance(typeof(TypeContext<>).MakeGenericType(entity.GetType()), entity, prefix);
        }

        public static TypeContext UntypedNew(ModifiableEntity entity, string prefix, PropertyRoute route)
        {
            return (TypeContext)Activator.CreateInstance(typeof(TypeContext<>).MakeGenericType(entity.GetType()), entity, (TypeContext)null, prefix, route);
        }

        public static string Compose(string prefix, string nameToAppend)
        {
            return prefix.Add(TypeContext.Separator, nameToAppend);
        }

        public static string Compose(string prefix, params string[] namesToAppend)
        {
            return Compose(prefix, (IEnumerable<string>)namesToAppend);
        }

        public static string Compose(string prefix, IEnumerable<string> namesToAppend)
        {
            return Compose(prefix, namesToAppend.ToString(TypeContext.Separator));
        }
    }
}
