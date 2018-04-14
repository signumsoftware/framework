using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Engine
{
    public static class DeletePart
    {
        static readonly Variable<ImmutableStack<Type>> avoidTypes = Statics.ThreadVariable<ImmutableStack<Type>>("avoidDeletePart");

        public static bool ShouldAvoidDeletePart(Type partType)
        {
            var stack = avoidTypes.Value;
            return stack != null && (stack.Contains(partType) || stack.Contains(null));
        }

        /// <param name="partType">Use null for every type</param>
        public static IDisposable AvoidDeletePart(Type partType)
        {
            avoidTypes.Value = (avoidTypes.Value ?? ImmutableStack<Type>.Empty).Push(partType);

            return new Disposable(() => avoidTypes.Value = avoidTypes.Value.Pop());
        }

        public static FluentInclude<T> WithDeletePart<T, L>(this FluentInclude<T> fi, Expression<Func<T, L>> relatedEntity)
            where T : Entity
            where L : Entity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (ShouldAvoidDeletePart(typeof(L)))
                    return null;

                var toDelete = query.Select(relatedEntity).Select(a => a.ToLite()).ToList().NotNull().Distinct().ToList();
                return new Disposable(() =>
                {
                    var groups = toDelete.GroupsOf(Connector.Current.Schema.Settings.MaxNumberOfParameters).ToList();
                    groups.ForEach(l => Database.DeleteList(l));
                });
            };
            return fi;
        }

        public static FluentInclude<T> WithDeletePart<T, L>(this FluentInclude<T> fi, Expression<Func<T, Lite<L>>> relatedEntity)
            where T : Entity
            where L : Entity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                var toDelete = query.Select(relatedEntity).ToList().NotNull().Distinct().ToList();;
                return new Disposable(() =>
                {
                    Database.DeleteList(toDelete);
                });
            };
            return fi;
        }
    }
}
