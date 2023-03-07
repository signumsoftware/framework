using Signum.Entities.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Discovery;

public static class DiscoveryLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<DiscoveryEntity>()
                .WithSave(DiscoveryOperation.Save)
                .WithDelete(DiscoveryOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.Type,
                    e.Related,
                });

            sb.Include<DiscoveryLogEntity>()
                  .WithQuery(() => e => new
                  {
                      Entity = e,
                      e.Id,
                      e.CreationDate,
                      e.User,
                      e.Discovery,
                  });
        }
    }
}
