using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Engine
{
    public static class DeletePart
    {
        static readonly Variable<ImmutableStack<Type?>> avoidTypes = Statics.ThreadVariable<ImmutableStack<Type?>>("avoidDeletePart"); /*CSBUG*/ 
 
        public static bool ShouldAvoidDeletePart(Type partType)
        {
            var stack = avoidTypes.Value;
            return stack != null && (stack.Contains(partType) || stack.Contains(null!));
        }

        /// <param name="partType">Use null for every type</param>
        public static IDisposable AvoidDeletePart(Type partType)
        {
            avoidTypes.Value = (avoidTypes.Value ?? ImmutableStack<Type?>.Empty).Push(partType);

            return new Disposable(() => avoidTypes.Value = avoidTypes.Value.Pop());
        }

        public static FluentInclude<T> WithDeletePart<T, L>(this FluentInclude<T> fi, Expression<Func<T, L>> relatedEntity, Expression<Func<T, bool>>? filter = null, Func<T, bool>? handleOnSaving = null)
            where T : Entity
            where L : Entity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (ShouldAvoidDeletePart(typeof(L)))
                    return null;

                var filteredQuery = filter == null ? query : query.Where(filter);

                var toDelete = filteredQuery.Select(relatedEntity).Select(a => a.ToLite()).ToList().NotNull().Distinct().ToList();
                return new Disposable(() =>
                {
                    var groups = toDelete.GroupsOf(Connector.Current.Schema.Settings.MaxNumberOfParameters).ToList();
                    groups.ForEach(l => Database.DeleteList(l));
                });
            };
            if (handleOnSaving != null)
                fi.SchemaBuilder.Schema.EntityEvents<T>().Saving += e =>
                {
                    if (!e.IsNew && handleOnSaving!(e))
                    {
                        var lite = e.InDB().Select(relatedEntity).Select(a => a.ToLite()).SingleEx();
                        if(!lite.Is(relatedEntity.Evaluate(e)))
                        {
                            Transaction.PreRealCommit += dic =>
                            {
                                lite.Delete();
                            };
                        }
                    }
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
