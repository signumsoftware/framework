using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Alerts;
using System.Linq.Expressions;
using Signum.Engine.Extensions.Basics;

namespace Signum.Engine.Alerts
{
    public static class AlertLogic
    {
        static Expression<Func<IdentifiableEntity, IQueryable<AlertDN>>> AlertsExpression =
            e => Database.Query<AlertDN>().Where(a => a.Target.RefersTo(e));
        public static IQueryable<AlertDN> Alerts(this IdentifiableEntity e)
        {
            return AlertsExpression.Evaluate(e);
        }

        public static HashSet<AlertTypeDN> SystemAlertTypes = new HashSet<AlertTypeDN>();
        static bool started = false;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Type[] registerExpressionsFor)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AlertDN>();

                dqm.RegisterQuery(typeof(AlertDN), () =>
                    from a in Database.Query<AlertDN>()
                    select new
                           {
                               Entity = a,
                               a.Id,
                               a.AlertType,
                               a.AlertDate,
                               Text = a.Text.Etc(100),
                               a.CreationDate,
                               a.CreatedBy,
                               a.AttendedDate,
                               a.AttendedBy,
                               a.Target
                           });

                AlertGraph.Register();

                dqm.RegisterQuery(typeof(AlertTypeDN), () =>
                    from t in Database.Query<AlertTypeDN>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        t.Key,
                    });

                SemiSymbolLogic<AlertTypeDN>.Start(sb, () => SystemAlertTypes);

                new Graph<AlertTypeDN>.Execute(AlertTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (a, _) => { }
                }.Register();

                if (registerExpressionsFor != null)
                {
                    var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((IdentifiableEntity ident) => ident.Alerts());
                    foreach (var type in registerExpressionsFor)
                        dqm.RegisterExpression(new ExtensionInfo(type, exp, exp.Body.Type, "Alerts", () => typeof(AlertDN).NicePluralName()));
                }

                started = true;
            }
        }

        public static void RegisterAlertType(AlertTypeDN alertType)
        {
            if (!alertType.Key.HasText())
                throw new InvalidOperationException("alertType must have a key, use MakeSymbol method after the constructor when declaring it");

            SystemAlertTypes.Add(alertType);
        }

        public static AlertDN CreateAlert(this IIdentifiable entity, string text, AlertTypeDN alertType, DateTime? alertDate = null, Lite<IUserDN> user = null, string title = null)
        {
            return CreateAlert(entity.ToLiteFat(), text, alertType, alertDate, user, title);
        }

        public static AlertDN CreateAlert<T>(this Lite<T> entity, string text, AlertTypeDN alertType, DateTime? alertDate = null, Lite<IUserDN> user = null, string title = null) where T : class, IIdentifiable
        {
            if (started == false)
                return null;

            var result = new AlertDN
            {
                AlertDate = alertDate ?? TimeZoneManager.Now,
                CreatedBy = user ?? UserHolder.Current.ToLite(),
                Text = text,
                Title = title,
                Target = (Lite<IdentifiableEntity>)entity,
                AlertType = alertType
            };

            return result.Execute(AlertOperation.SaveNew);
        }

        public static AlertDN CreateAlertForceNew(this IIdentifiable entity, string text, AlertTypeDN alertType, DateTime? alertDate = null, Lite<IUserDN> user = null)
        {
            return CreateAlertForceNew(entity.ToLite(), text, alertType, alertDate, user);
        }

        public static AlertDN CreateAlertForceNew<T>(this Lite<T> entity, string text, AlertTypeDN alertType, DateTime? alertDate = null, Lite<IUserDN> user = null) where T : class, IIdentifiable
        {
            if (started == false)
                return null;

            using (Transaction tr = Transaction.ForceNew())
            {
                var alerta = entity.CreateAlert(text, alertType, alertDate, user);

                return tr.Commit(alerta);
            }
        }
    }

    public class AlertGraph : Graph<AlertDN, AlertState>
    {
        public static void Register()
        {
            GetState = a => a.State;

            new ConstructFrom<IdentifiableEntity>(AlertOperation.CreateAlertFromEntity)
            {
                ToState = AlertState.New,
                Construct = (a, _) => new AlertDN
                {
                    AlertDate = TimeZoneManager.Now,
                    CreatedBy = UserHolder.Current.ToLite(),
                    Text = null,
                    Title = null,
                    Target = a.ToLite(),
                    AlertType = null
                }
            }.Register();

            new Execute(AlertOperation.SaveNew)
            {
                FromStates = { AlertState.New },
                ToState = AlertState.Saved,
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Save)
            {
                FromStates = { AlertState.Saved },
                ToState = AlertState.Saved,
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Attend)
            {
                FromStates = { AlertState.Saved },
                ToState = AlertState.Attended,
                Execute = (a, _) =>
                {
                    a.State = AlertState.Attended;
                    a.AttendedDate = TimeZoneManager.Now;
                    a.AttendedBy = UserDN.Current.ToLite();
                }
            }.Register();

            new Execute(AlertOperation.Unattend)
            {
                FromStates = { AlertState.Attended },
                ToState = AlertState.Saved,
                Execute = (a, _) =>
                {
                    a.State = AlertState.Saved;
                    a.AttendedDate = null;
                    a.AttendedBy = null;
                }
            }.Register();
        }
    }

 
}
