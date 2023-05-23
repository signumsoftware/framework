using Signum.DiffLog;
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
}
