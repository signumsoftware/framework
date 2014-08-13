using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Engine.Basics;

namespace Signum.Engine.UserAssets
{
    public static class ViewLogLogic
    {
        static Expression<Func<IdentifiableEntity, IQueryable<ViewLogDN>>> ViewLogsExpression =
            a => Database.Query<ViewLogDN>().Where(log => log.Target.RefersTo(a));
        public static IQueryable<ViewLogDN> ViewLogs(this IdentifiableEntity a)
        {
            return ViewLogsExpression.Evaluate(a);
        }

        public static HashSet<Type> Types = new HashSet<Type>();

        static bool Started;
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, HashSet<Type> types)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Started = true;

                sb.Include<ViewLogDN>();

                dqm.RegisterQuery(typeof(ViewLogDN), () =>
                    from e in Database.Query<ViewLogDN>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Target,
                        e.User,
                        e.Duration,
                        e.StartDate,
                        e.EndDate,
                    });

                Types = types;

                var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((IdentifiableEntity entity)=> entity.ViewLogs());

                foreach (var t in types)
                {
                    dqm.RegisterExpression(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", ()=>typeof(ViewLogDN).NicePluralName()));
                }

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        static void ExceptionLogic_DeleteLogs(DateTime limit)
        {
            Database.Query<ViewLogDN>().Where(view => view.StartDate < limit);
        }

        public static IDisposable LogView(IIdentifiable entity, string viewAction)
        {
            if (Started && Types.Contains(entity.GetType()))
                return null;

            var viewLog = new ViewLogDN
            {
                Target = ((IdentifiableEntity)entity).ToLite(),
                User = UserHolder.Current.ToLite(),
                ViewAction = viewAction,
            };

            return new Disposable(() =>
            {
                viewLog.EndDate = TimeZoneManager.Now;
                using (ExecutionMode.Global())
                    viewLog.Save();
            });
        }
    }
   
}
