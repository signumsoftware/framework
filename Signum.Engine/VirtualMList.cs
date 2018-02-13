using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
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
         Expression<Func<T, MList<L>>> mListField,
         Expression<Func<L, Lite<T>>> getBackReference,
         ExecuteSymbol<L> saveOperation,
         DeleteSymbol<L> deleteOperation)
            where T : Entity
            where L : Entity
        {

            return fi.WithVirtualMList(mListField, getBackReference,
                onSave: saveOperation == null ? null : new Action<L, T>((line, e) =>
                {
                    line.Execute(saveOperation);
                }),
                onRemove: deleteOperation == null ? null : new Action<L, T>((line, e) =>
                {
                    line.Delete(deleteOperation);
                }));
        }

        public static FluentInclude<T> WithVirtualMList<T, L>( this FluentInclude<T> fi, 
            Expression<Func<T, MList<L>>> mListField, 
            Expression<Func<L, Lite<T>>> getBackReference, 
            Action<L, T> onSave = null,
            Action<L, T> onRemove = null)
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
                query.SelectMany(e => Database.Query<L>().Where(se => getBackReference.Evaluate(se).RefersTo(e))).UnsafeDelete();
                return null;
            };

            return fi;
        }

        public static FluentInclude<T> WithVirtualMListInitializeOnly<T, L>(this FluentInclude<T> fi,
            Func<T, MList<L>> getMList,
            Expression<Func<L, Lite<T>>> getBackReference,
            Action<L, T> onSave = null)
            where T : Entity
            where L : Entity
        {
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
}
