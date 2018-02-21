using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine
{
    
    public static class VirtualMList
    {
        public static FluentInclude<T> WithVirtualMList<T, L>(this FluentInclude<T> fi,
            DynamicQueryManager dqm,
            Expression<Func<T, MList<L>>> mListField,
            Expression<Func<L, Lite<T>>> getBackReference,
            ExecuteSymbol<L> saveOperation,
            DeleteSymbol<L> deleteOperation)
            where T : Entity
            where L : Entity
        {

            return fi.WithVirtualMList(dqm, mListField, getBackReference,
                onSave: saveOperation == null ? null : new Action<L, T>((line, e) =>
                {
                    line.Execute(saveOperation);
                }),
                onRemove: deleteOperation == null ? null : new Action<L, T>((line, e) =>
                {
                    line.Delete(deleteOperation);
                }));
        }

        public static FluentInclude<T> WithVirtualMList<T, L>(this FluentInclude<T> fi,
            DynamicQueryManager dqm, 
            Expression<Func<T, MList<L>>> mListField, 
            Expression<Func<L, Lite<T>>> getBackReference, 
            Action<L, T> onSave = null,
            Action<L, T> onRemove = null,
            bool? checkAnyBeforeDelete = null) //To avoid StackOverflows
            where T : Entity
            where L : Entity
        {
            Func<T, MList<L>> getMList = mListField.Compile();
            Action<L, Lite<T>> setter = null;
            bool preserveOrder = fi.SchemaBuilder.Settings.FieldAttributes(mListField)
                .OfType<PreserveOrderAttribute>()
                .Any();
            var sb = fi.SchemaBuilder;
            sb.Schema.EntityEvents<T>().Retrieved += (T e) =>
            {
                var mlist = getMList(e);

                List<L> list = Database.Query<L>()
                    .Where(line => getBackReference.Evaluate(line) == e.ToLite())
                    .ToList();
                if (preserveOrder)
                {
                    list = list.OrderBy(le => ((ICanBeOrdered)le).Order).ToList();
                } 

                var rowIdElements = list
                    .Select(line => new MList<L>.RowIdElement(line, line.Id, null));

                ((IMListPrivate<L>)mlist).InnerList.AddRange(rowIdElements);
                ((IMListPrivate<L>)mlist).InnerListModified(rowIdElements.Select(a => a.Element).ToList(), null);
            };

            sb.Schema.EntityEvents<T>().Saving += (T e) =>
            {
                var mlist = getMList(e);
                if (preserveOrder)
                {
                    mlist.ForEach((o,i) => ((ICanBeOrdered) o).Order = i);
                }

                if (GraphExplorer.IsGraphModified(mlist))
                    e.SetModified();
            };
            sb.Schema.EntityEvents<T>().Saved += (T e, SavedEventArgs args) =>
            {
                var mlist = getMList(e);

                if (!GraphExplorer.IsGraphModified(mlist))
                    return;

                if (!args.WasNew)
                {
                    var oldElements = mlist.Where(line => !line.IsNew);
                    var query = Database.Query<L>()
                    .Where(p => getBackReference.Evaluate(p) == e.ToLite());

                    if(onRemove == null)
                        query.Where(p => !oldElements.Contains(p)).UnsafeDelete();
                    else
                        query.ToList().ForEach(line => onRemove(line, e));
                }

                if (setter == null)
                    setter = CreateSetter(getBackReference);

                mlist.ForEach(line => setter(line, e.ToLite()));
                if (onSave == null)
                    mlist.SaveList();
                else
                    mlist.ForEach(line => { if (GraphExplorer.IsGraphModified(line)) onSave(line, e); });
                var priv = (IMListPrivate)mlist;
                for (int i = 0; i < mlist.Count; i++)
                {
                    if (priv.GetRowId(i) == null)
                        priv.SetRowId(i, mlist[i].Id);
                }
                mlist.SetCleanModified(false);
            };

            
            sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                //You can do a VirtualMList to itself at the table level, but there should not be cycles inside the instances
                var toDelete = Database.Query<L>().Where(se => query.Any(e => getBackReference.Evaluate(se).RefersTo(e)));
                if (checkAnyBeforeDelete ?? (typeof(L) == typeof(T)))
                {
                    if (toDelete.Any())
                        toDelete.UnsafeDelete();
                }
                else
                {
                    toDelete.UnsafeDelete();
                }
                return null;
            };

            if (dqm != null)
            {
                var pi = ReflectionTools.GetPropertyInfo(mListField);
                dqm.RegisterExpression((T e) => Database.Query<L>().Where(p => getBackReference.Evaluate(p) == e.ToLite()), () => pi.NiceName(), pi.Name);
            }
            return fi;
        }

        public static FluentInclude<T> WithVirtualMListInitializeOnly<T, L>(this FluentInclude<T> fi,
            DynamicQueryManager dqm,
           Expression<Func<T, MList<L>>> mListField,
            Expression<Func<L, Lite<T>>> getBackReference,
            Action<L, T> onSave = null)
            where T : Entity
            where L : Entity
        {
            Func<T, MList<L>> getMList = mListField.Compile();
            Action<L, Lite<T>> setter = null;
            var sb = fi.SchemaBuilder;

            sb.Schema.EntityEvents<T>().Saving += (T e) =>
            {
                if (GraphExplorer.IsGraphModified(getMList(e)))
                    e.SetModified();
            };
            sb.Schema.EntityEvents<T>().Saved += (T e, SavedEventArgs args) =>
            {
                var mlist = getMList(e);

                if (!GraphExplorer.IsGraphModified(mlist))
                    return;

                if (setter == null)
                    setter = CreateSetter(getBackReference);

                mlist.ForEach(line => setter(line, e.ToLite()));
                if (onSave == null)
                    mlist.SaveList();
                else
                    mlist.ForEach(line => { if (GraphExplorer.IsGraphModified(line)) onSave(line, e); });
                var priv = (IMListPrivate)mlist;
                for (int i = 0; i < mlist.Count; i++)
                {
                    if (priv.GetRowId(i) == null)
                        priv.SetRowId(i, mlist[i].Id);
                }
                mlist.SetCleanModified(false);
            };
            if (dqm != null)
            {
                var pi = ReflectionTools.GetPropertyInfo(mListField);
                dqm.RegisterExpression((T e) => Database.Query<L>().Where(p => getBackReference.Evaluate(p) == e.ToLite()), () => pi.NiceName(), pi.Name);
            }
            return fi;
        }

        private static Action<L, Lite<T>> CreateSetter<T, L>(Expression<Func<L, Lite<T>>> getBackReference)
            where T : Entity
            where L : Entity
        {
            var body = getBackReference.Body;
            if (body.NodeType == ExpressionType.Convert)
                body = ((UnaryExpression)body).Operand;
            
            return ReflectionTools.CreateSetter<L, Lite<T>>(((MemberExpression)body).Member);
        }
    }

    public static class DeletePart
    {
        static readonly Variable<ImmutableStack<Type>> avoidTypes = Statics.ThreadVariable<ImmutableStack<Type>>("avoidDeletePart");

        public static bool ShouldAvoidDeletePart(Type type)
        {
            var stack = avoidTypes.Value;
            return (stack != null && stack.Contains(type));
        }

        public static IDisposable AvoidDeletePart(Type type)
        {
            avoidTypes.Value = (avoidTypes.Value ?? ImmutableStack<Type>.Empty).Push(type);

            return new Disposable(() => avoidTypes.Value = avoidTypes.Value.Pop());
        }

        public static FluentInclude<T> WithDeletePart<T, L>(this FluentInclude<T> fi, Expression<Func<T, L>> relatedEntity)
            where T : Entity
            where L : Entity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
            {
                if (ShouldAvoidDeletePart(typeof(T)))
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
