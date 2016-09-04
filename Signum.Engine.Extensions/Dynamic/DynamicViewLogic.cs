using Signum.Engine;
using Signum.Engine.Basics;
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
    public static class DynamicViewLogic
    {
        public static ResetLazy<Dictionary<Type, List<DynamicViewEntity>>> DynamicViews;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<DynamicViewEntity>()
                    .WithUniqueIndex(a => new { a.ViewName, a.EntityType })
                    .WithSave(DynamicViewOperation.Save)
                    .WithDelete(DynamicViewOperation.Delete)
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.ViewName,
                        e.EntityType,
                    });

                new Graph<DynamicViewEntity>.ConstructFrom<DynamicViewEntity>(DynamicViewOperation.Clone)
                {
                    Construct = (e, _) => new DynamicViewEntity()
                    {
                        ViewName = "",
                        EntityType = e.EntityType,
                        ViewContent = e.ViewContent,
                    },
                }.Register();

                DynamicViews = sb.GlobalLazy(() =>
                    Database.Query<DynamicViewEntity>().GroupToDictionary(a => a.EntityType.ToType()),
                    new InvalidateWith(typeof(DynamicViewEntity)));
            }
        }
    }
}
