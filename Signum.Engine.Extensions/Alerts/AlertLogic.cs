using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Alerts;
using System.Linq.Expressions;
using Signum.Engine.Extensions.Basics;
using Signum.Engine.Basics;
using Signum.Engine.Authorization;
using Signum.Engine.Mailing;
using Signum.Entities.Mailing;
using Signum.Engine.Templating;
using Signum.Engine.Scheduler;
using Signum.Entities.UserAssets;
using Microsoft.AspNetCore.Html;
using Signum.Engine;
using Signum.Entities.Scheduler;

namespace Signum.Engine.Alerts
{
    public static class AlertLogic
    {
        [AutoExpressionField]
        public static IQueryable<AlertEntity> Alerts(this Entity e) => 
            As.Expression(() => Database.Query<AlertEntity>().Where(a => a.Target.Is(e)));

        [AutoExpressionField]
        public static IQueryable<AlertEntity> MyActiveAlerts(this Entity e) => 
            As.Expression(() => e.Alerts().Where(a => a.Recipient == UserHolder.Current.ToLite() && a.CurrentState == AlertCurrentState.Alerted));

        public static Func<IUserEntity?> DefaultRecipient = () => null;

        public static HashSet<AlertTypeEntity> SystemAlertTypes = new HashSet<AlertTypeEntity>();
        public static bool Started = false;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!, null!)));
        }

        public static void Start(SchemaBuilder sb, params Type[] registerExpressionsFor)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<AlertEntity>()
                    .WithQuery(() => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.AlertDate,
                        a.AlertType,
                        a.State,
                        a.Title,
                        Text = a.Text.Etc(100),
                        a.Target,
                        a.Recipient,
                        a.CreationDate,
                        a.CreatedBy,
                        a.AttendedDate,
                        a.AttendedBy,
                    });

                AlertGraph.Register();

                

                sb.Include<AlertTypeEntity>()
                    .WithSave(AlertTypeOperation.Save)
                    .WithDelete(AlertTypeOperation.Delete)
                    .WithQuery(() => t => new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        t.Key,
                    });

                SemiSymbolLogic<AlertTypeEntity>.Start(sb, () => SystemAlertTypes);

                if (registerExpressionsFor != null)
                {
                    var alerts = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity ident) => ident.Alerts());
                    var myActiveAlerts = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity ident) => ident.MyActiveAlerts());
                    foreach (var type in registerExpressionsFor)
                    {
                        QueryLogic.Expressions.Register(new ExtensionInfo(type, alerts, alerts.Body.Type, "Alerts", () => typeof(AlertEntity).NicePluralName()));
                        QueryLogic.Expressions.Register(new ExtensionInfo(type, myActiveAlerts, myActiveAlerts.Body.Type, "MyActiveAlerts", () => AlertMessage.MyActiveAlerts.NiceToString()));
                    }
                }
                

                Started = true;
            }
        }

        public static void RegisterAlertNotificationMail(SchemaBuilder sb)
        {
            EmailModelLogic.RegisterEmailModel<AlertNotificationMail>(() => new EmailTemplateEntity
            {
                Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
                {
                    Text = @"
<p>Hi @[m:Entity],</p>
<p>You have some pending alerts:</p>
<ul>
 @foreach[m:Alerts] as $a
    <li>
        <strong>@[$a.AlertType]:</strong><br/>
        @[m:TextFormatted]<br/>
        <small>@[$a.AlertDate] @[$a.CreatedBy]</small>
    </li>
 @endforeach
</ul>
<p>Please visit <a href=""@[g:UrlLeft]"">@[g:UrlLeft]</a></p>",
                    Subject = AlertMessage.NewUnreadNotifications.NiceToString(),
                }).ToMList()
            });

            sb.Include<SendNotificationEmailTaskEntity>()
                  .WithSave(SendNotificationEmailTaskOperation.Save)
                  .WithQuery(() => e => new
                  {
                      Entity = e,
                      e.Id,
                      e.SendNotificationsOlderThan,
                      e.SendBehavior,
                  });

            SchedulerLogic.ExecuteTask.Register((SendNotificationEmailTaskEntity task, ScheduledTaskContext ctx) =>
            {
                var limit = DateTime.Now.AddMinutes(-task.SendNotificationsOlderThan);

                var query = Database.Query<AlertEntity>()
                .Where(a => a.State == AlertState.Saved && a.EmailNotificationsSent == 0 && a.Recipient != null && a.CreationDate < limit)
                .Where(a => task.SendBehavior == SendAlertTypeBehavior.All ||
                            task.SendBehavior == SendAlertTypeBehavior.Include && task.AlertTypes.Contains(a.AlertType!) ||
                            task.SendBehavior == SendAlertTypeBehavior.Exclude && !task.AlertTypes.Contains(a.AlertType!));

                if (!query.Any())
                    return null;

                var alerts = query
                .Select(a => new { Alert = a, Recipient = a.Recipient!.Entity })
                .ToList();

                EmailPackageEntity emailPackage = new EmailPackageEntity().Save();

                var emails = alerts.GroupBy(a => a.Recipient, a => a.Alert).SelectMany(gr => new AlertNotificationMail((UserEntity)gr.Key, gr.ToList()).CreateEmailMessage()).ToList();

                emails.ForEach(a =>
                {
                    a.State = EmailMessageState.ReadyToSend;
                    a.Package = emailPackage.ToLite();
                });

                emails.BulkInsertQueryIds(a => a.Target!);

                query.UnsafeUpdate().Set(a => a.EmailNotificationsSent, a => 1).Execute();

                return emailPackage.ToLite();
            });
        }

        public class AlertNotificationMail : EmailModel<UserEntity>
        {
            public List<AlertEntity> Alerts { get; set; }

            public AlertNotificationMail(UserEntity recipient, List<AlertEntity> alerts) : base(recipient)
            {
                this.Alerts = alerts;
            }

            public HtmlString? TextFormatted(TemplateParameters tp)
            {
                if (!tp.RuntimeVariables.TryGetValue("$a", out object? alertObject))
                    return null;

                var alert = (AlertEntity)alertObject;
                var text = alert.Text;

                if (alert.Target == null)
                    return new HtmlString(text);

                var url=  $"{EmailLogic.Configuration.UrlLeft}/view/{TypeLogic.GetCleanName(alert.Target.EntityType)}/{alert.Target.Id}";

                if (text.Contains("[Target]"))
                    return new HtmlString(@$"{text.Before("[Target]")}<a href=""{url}"">{alert.Target.ToString()}</a>{text.After("[Target]")}");

                if (text.Contains("[Target:"))
                    return new HtmlString(@$"{text.Before("[Target:")}<a href=""{url}"">{text.After("[Target:").BeforeLast("]")}</a>{text.AfterLast("[Target]")}");

                return new HtmlString(@$"{text}<br/><a href=""{url}"">{alert.Target.ToString()}</a>");
            }

            public override List<EmailOwnerRecipientData> GetRecipients()
            {
                return new List<EmailOwnerRecipientData>
                {
                    new EmailOwnerRecipientData(this.Entity.EmailOwnerData)
                    {
                        Kind = EmailRecipientKind.To,
                    }
                };
            }
        }

        public static void RegisterAlertType(AlertTypeEntity alertType)
        {
            if (!alertType.Key.HasText())
                throw new InvalidOperationException("alertType must have a key, use MakeSymbol method after the constructor when declaring it");

            SystemAlertTypes.Add(alertType);
        }

        public static AlertEntity? CreateAlert(this IEntity entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null)
        {
            return CreateAlert(entity.ToLiteFat(), text, alertType, alertDate, createdBy, title, recipient);
        }

        public static AlertEntity? CreateAlert<T>(this Lite<T> entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null) where T : class, IEntity
        {
            if (Started == false)
                return null;

            var result = new AlertEntity
            {
                AlertDate = alertDate ?? TimeZoneManager.Now,
                CreatedBy = createdBy ?? UserHolder.Current?.ToLite(),
                Text = text,
                Title = title,
                Target = (Lite<Entity>)entity,
                AlertType = alertType,
                Recipient = recipient
            };

            return result.Execute(AlertOperation.Save);
        }

        public static AlertEntity? CreateAlertForceNew(this IEntity entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null)
        {
            return CreateAlertForceNew(entity.ToLite(), text, alertType, alertDate, createdBy, title, recipient);
        }

        public static AlertEntity? CreateAlertForceNew<T>(this Lite<T> entity, string text, AlertTypeEntity alertType, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null) where T : class, IEntity
        {
            if (Started == false)
                return null;

            using (Transaction tr = Transaction.ForceNew())
            {
                var alert = entity.CreateAlert(text, alertType, alertDate, createdBy);

                return tr.Commit(alert);
            }
        }

        public static void RegisterCreatorTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((AlertEntity a) => a.CreatedBy, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<AlertEntity>(typeCondition,
                a => a.CreatedBy.Is(UserEntity.Current));
        }

        public static void RegisterRecipientTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((AlertEntity a) => a.Recipient, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<AlertEntity>(typeCondition,
                a => a.Recipient.Is(UserEntity.Current));
        }

        public static void AttendAllAlerts(Lite<Entity> target, AlertTypeEntity alertType)
        {
            using (AuthLogic.Disable())
            {
                Database.Query<AlertEntity>()
                    .Where(a => a.Target.Is(target) && a.AlertType == alertType && a.State == AlertState.Saved)
                    .ToList()
                    .ForEach(a => a.Execute(AlertOperation.Attend));
            }
        }

        public static void DeleteAllAlerts(Lite<Entity> target)
        {
            using (AuthLogic.Disable())
            {
                Database.Query<AlertEntity>()
                    .Where(a => a.Target.Is(target))
                    .UnsafeDelete();
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
                    Recipient = AlertLogic.DefaultRecipient()?.ToLite(),
                    Text = "",
                    Title = null,
                    Target = a.ToLite(),
                    AlertType = null
                }
            }.Register();

            new Construct(AlertOperation.Create)
            {
                ToStates = { AlertState.New },
                Construct = (_) => new AlertEntity
                {
                    AlertDate = TimeZoneManager.Now,
                    CreatedBy = UserHolder.Current.ToLite(),
                    Recipient = AlertLogic.DefaultRecipient()?.ToLite(),
                    Text = "",
                    Title = null,
                    Target = null!,
                    AlertType = null
                }
            }.Register();

            new Execute(AlertOperation.Save)
            {
                FromStates = { AlertState.Saved, AlertState.New },
                ToStates = { AlertState.Saved },
                CanBeNew = true,
                CanBeModified = true,
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

            new Execute(AlertOperation.Delay)
            {
                FromStates = { AlertState.Saved },
                ToStates = { AlertState.Saved },
                Execute = (a, args) =>
                {
                    a.AlertDate = args.GetArg<DateTime>();
                }
            }.Register();
        }
    }
}
