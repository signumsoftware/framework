using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Dynamic
{

    public static class DynamicCSSOverrideLogic
    {
        public static ResetLazy<List<DynamicCSSOverrideEntity>> Cached;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodBase.GetCurrentMethod()))
            {
                sb.Include<DynamicCSSOverrideEntity>()
                   .WithSave(DynamicCSSOverrideOperation.Save)
                   .WithDelete(DynamicCSSOverrideOperation.Delete)
                   .WithQuery(() => e => new
                   {
                       Entity = e,
                       e.Id,
                       e.Name,
                       Script = e.Script.Etc(100),
                   });

                Cached = sb.GlobalLazy(() =>
                 Database.Query<DynamicCSSOverrideEntity>().Where(a => !a.Mixin<DisabledMixin>().IsDisabled).ToList(),
                 new InvalidateWith(typeof(DynamicCSSOverrideEntity)));
            }
        }
    }
}
