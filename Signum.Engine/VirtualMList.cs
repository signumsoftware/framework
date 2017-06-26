using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
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
         Func<T, MList<L>> getMList,
         Expression<Func<L, Lite<T>>> getBackReference,
         ExecuteSymbol<L> saveOperation,
         DeleteSymbol<L> deleteOperation)
            where T : Entity
            where L : Entity
        {
            return fi.WithVirtualMList(getMList, getBackReference,
                onSave: saveOperation == null ? null : new Action<L, T>((line, e) =>
                 {
                     if (line.IsGraphModified)
                         line.Execute(saveOperation);
                 }),
                onRemove: deleteOperation == null ? null : new Action<L, T>((line, e) =>
                {
                    line.Delete(deleteOperation);
                }));
        }

        public static FluentInclude<T> WithVirtualMList<T, L>( this FluentInclude<T> fi, 
            Func<T, MList<L>> getMList, 
            Expression<Func<L, Lite<T>>> getBackReference, 
            Action<L, T> onSave = null,
            Action<L, T> onRemove = null)
            where T : Entity
            where L : Entity
        {
            Action<L, Lite<T>> setter = null; 
            var sb = fi.SchemaBuilder;
            sb.Schema.EntityEvents<T>().Retrieved += (T e) =>
            {
                var mlist = getMList(e);

                var rowIdElements = Database.Query<L>()
                    .Where(line => getBackReference.Evaluate(line) == e.ToLite())
                    .ToList()
                    .Select(line => new MList<L>.RowIdElement(line, line.Id, null));

                ((IMListPrivate<L>)mlist).InnerList.AddRange(rowIdElements);
            };

            sb.Schema.EntityEvents<T>().Saving += (T e) =>
            {
                if (getMList(e).IsGraphModified)
                    e.SetModified();
            };
            sb.Schema.EntityEvents<T>().Saved += (T e, SavedEventArgs args) =>
            {
                var mlist = getMList(e);
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
                    setter = ReflectionTools.CreateSetter<L, Lite<T>>(((MemberExpression)getBackReference.Body).Member);

                mlist.ForEach(line => setter(line, e.ToLite()));
                if (onSave == null)
                    mlist.SaveList();
                else
                    mlist.ForEach(line => onSave(line, e));
                var priv = (IMListPrivate)mlist;
                for (int i = 0; i < mlist.Count; i++)
                {
                    if (priv.GetRowId(i) == null)
                        priv.SetRowId(i, mlist[i].Id);
                }
            };

            return fi;
        }
    }
}
