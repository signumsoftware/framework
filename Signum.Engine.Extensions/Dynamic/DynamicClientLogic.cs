using Signum.Engine.Cache;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Signum.Engine.Dynamic
{
    public static class DynamicClientLogic
    {
        public static ResetLazy<List<DynamicClientEntity>> Clients = null!;

        public static bool IsStarted = false;
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicClientEntity>()
                    .WithSave(DynamicClientOperation.Save)
                    .WithDelete(DynamicClientOperation.Delete)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        Code = e.Code.Etc(50),
                    });

                new Graph<DynamicClientEntity>.ConstructFrom<DynamicClientEntity>(DynamicClientOperation.Clone)
                {
                    Construct = (e, _) =>
                    {
                        return new DynamicClientEntity
                        {
                            Name = e.Name + "_2",
                            Code = e.Code,
                        };
                    }
                }.Register();

                Clients = sb.GlobalLazy(() => Database.Query<DynamicClientEntity>().Where(a => !a.Mixin<DisabledMixin>().IsDisabled).ToList(), 
                    new InvalidateWith(typeof(DynamicClientEntity)));

                IsStarted = true;
            }
        }
    }
}
