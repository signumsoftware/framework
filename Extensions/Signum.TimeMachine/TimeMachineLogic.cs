using Signum.Authorization;
using Signum.DiffLog;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.TimeMachine;

public static class TimeMachineLogic
{


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(TimeMachinePermission));

            if (sb.WebServerBuilder != null)
                TimeMachineServer.Start(sb.WebServerBuilder.WebApplication);
        }
    }

    public static void RestoreDeletedEntity<T>(PrimaryKey id)
        where T : Entity
    {
        var lastVersion = SystemTime.Override(new SystemTime.All(JoinBehaviour.AllCompatible))
            .Using(_ => Database.Query<T>().Where(a => a.Id == id).Max(a => a.SystemPeriod().Max))!.Value;

        RestoreDeletedEntity<T>(id, lastVersion.AddMicroseconds(-10));
    }

    private static void RestoreDeletedEntity<T> (PrimaryKey id, DateTime lastVersion)
        where T : Entity
    {
        using (var tr = new Transaction())
        {
            T entity;
            using (SystemTime.Override(new SystemTime.AsOf(lastVersion)))
            {
                entity = Database.Retrieve<T>(id);
            }

            RestoreEntity(entity);

            tr.Commit();
        }

    }

    private static void RestoreEntity(Entity entity)
    {
        foreach (var item in GraphExplorer.FromRoot(entity).CompilationOrder().OfType<Entity>())
        {
            if (!Database.Exists(item.ToLite()))
            {
                item.SetModified();
                item.SetIsNew();
                Administrator.SaveDisableIdentity(item);

                //not tested
                if (GraphExplorer.FromRootEntity(entity).OfType<IMListPrivate>().Any())
                {
                    var mlists = PropertyRoute.GenerateRoutes(item.GetType(), includeIgnored: false).Where(a => a.Type.IsMList());

                    foreach (var prMList in mlists)
                    {
                        giInsertMListElements.GetInvoker(item.GetType(), prMList.Type.ElementType()!)(item, prMList);
                    }
                }
            }

            var dic = VirtualMList.RegisteredVirtualMLists.TryGetC(item.GetType());
            if(dic != null)
            {
                foreach (var kvp in dic)
                {
                    var mlist = kvp.Value.GetMList(item);
                    if(mlist != null)
                    {
                        foreach (var e in (IEnumerable<IEntity>)mlist)
                        {
                            RestoreEntity((Entity)e);
                        }
                    }
                }
            }
        }
    }

    static GenericInvoker<Action<Entity, PropertyRoute>> giInsertMListElements = 
        new GenericInvoker<Action<Entity, PropertyRoute>>((e, pr) => InsertMListElements<UserEntity, string>((UserEntity)e, pr));

    private static void InsertMListElements<E, L>(E item, PropertyRoute prMList)
        where E : Entity
    {
        var ex = prMList.GetLambdaExpression<E, MList<L>>(safeNullAccess: true);

        var mlist = ex.Compile()(item);

        if (mlist == null)
            return;

        var elements = ((IMListPrivate<L>)mlist).InnerList.Select(rid => new MListElement<E, L>
        {
            RowId = rid.RowId!.Value,
            Element = rid.Element,
            Parent = item,
            RowOrder = rid.OldIndex ?? 0
        });

        var mlistExpression = prMList.GetLambdaExpression<E, MList<L>>(safeNullAccess: false);

        //BulkInsert becahse there is no way to DisableIdentity for the MListTable's RowId column

        BulkInserter.BulkInsertMListTable(elements, mlistExpression, disableMListIdentity: true);
    }
}
