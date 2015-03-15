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
        static Expression<Func<Entity, IQueryable<AlertEntity>>> AlertsExpression =
            e => Database.Query<AlertEntity>().Where(a => a.Target.RefersTo(e));
        public static IQueryable<AlertEntity> Alerts(this Entity e)
        {
            return AlertsExpression.Evaluate(e);
        }

        public static HashSet<AlertTypeEntity> SystemAlertTypes = new HashSet<AlertTypeEntity>();
        static bool started = false;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null, null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, params Type[] registerExpressionsFor)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AlertEntity>();

                dqm.RegisterQuery(typeof(AlertEntity), () =>
                    from a in Database.Query<AlertEntity>()
                    select new
                           {
                               Entity = a,
                               a.Id,
                               a.AlertType,
                               a.AlertDate,
                               a.Title,
                               Text = a.Text.Etc(100),
                               a.CreationDate,
                               a.CreatedBy,
                               a.AttendedDate,
                               a.AttendedBy,
                               a.Target
                           });

                AlertGraph.Register();

                dqm.RegisterQuery(typeof(AlertTypeEntity), () =>
                    from t in Database.Query<AlertTypeEntity>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        t.Key,
                    });

                SemiSymbolLogic<AlertTypeEntity>.Start(sb, () => SystemAlertTypes);

                new Graph<AlertTypeEntity>.Execute(AlertTypeOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (a, _) => { }
                }.Register();

                if (registerExpressionsFor != null)
                {
                    var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity ident) => ident.Alerts());
                    foreach (var type in registerExpressionsFor)
                        dqm.RegisterExpression(new ExtensionInfo(type, exp, exp.Body.Type, "Alerts", () => typeof(AlertEntity).NicePluralName()));
                }

                started = true;
            }
        }

        public static void RegisterAlertType(AlertTypeEntity alertType)
        {
            if (!alertType.Key.HasText())
                throw new InvalidOperationException("alertType must have a key, use MakeSymbol method after the constructor when declaring it");

            SystemAlertTypes.Add(alertType);
        }

        public static AlertEntity CreateAlert(this IEntity entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity> user = null, string title = null)
        {
            return CreateAlert(entity.ToLiteFat(), text, alertType, alertDate, user, title);
        }

        public static AlertEntity CreateAlert<T>(this Lite<T> entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity> user = null, string title = null) where T : class, IEntity
        {
            if (started == false)
                return null;

            var result = new AlertEntity
            {
                AlertDate = alertDate ?? TimeZoneManager.Now,
                CreatedBy = user ?? UserHolder.Current.ToLite(),
                Text = text,
                Title = title,
                Target = (Lite<Entity>)entity,
                AlertType = alertType
            };

            return result.Execute(AlertOperation.SaveNew);
        }

        public static AlertEntity CreateAlertForceNew(this IEntity entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity> user = null)
        {
            return CreateAlertForceNew(entity.ToLite(), text, alertType, alertDate, user);
        }

        public static AlertEntity CreateAlertForceNew<T>(this Lite<T> entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity> user = null) where T : class, IEntity
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

    public class AlertGraph : Graph<AlertEntity, AlertState>
    {
        public static void Register()
        {
            GetState = a => a.State;

            new ConstructFrom<Entity>(AlertOperation.CreateAlertFromEntity)
            {
                ToStates = { AlertState.New },
                Construct = (a, _) => new AlertEntity
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
                ToStates = { AlertState.Saved },
                AllowsNew = true,
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Save)
            {
                FromStates = { AlertState.Saved },
                ToStates = { AlertState.Saved },
                Lite = false,
                Execute = (a, _) => { a.State = AlertState.Saved; }
            }.Register();

            new Execute(AlertOperation.Attend)
            {
                FromStates = { AlertState.Saved },
                ToStates = { AlertState.Attended },
                Execute = (a, _) =>
                {
                    a.State = AlertState.Attended;
                    a.AttendedDate = TimeZoneManager.Now;
                    a.AttendedBy = UserEntity.Current.ToLite();
                }
            }.Register();

            new Execute(AlertOperation.Unattend)
            {
                FromStates = { AlertState.Attended },
                ToStates = { AlertState.Saved },
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
