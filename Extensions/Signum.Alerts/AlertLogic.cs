using Microsoft.AspNetCore.Html;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Engine.Sync;
using Signum.Mailing;
using Signum.Mailing.Package;
using Signum.Mailing.Templates;
using Signum.Scheduler;
using Signum.Templating;
using Signum.UserAssets;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;

namespace Signum.Alerts;

public static class AlertLogic
{
    [AutoExpressionField]
    public static IQueryable<AlertEntity> Alerts(this Entity e) => 
        As.Expression(() => Database.Query<AlertEntity>().Where(a => a.Target.Is(e)));

    [AutoExpressionField]
    public static IQueryable<AlertEntity> MyActiveAlerts(this Entity e) => 
        As.Expression(() => e.Alerts().Where(a => a.Recipient.Is(UserHolder.Current.User) && a.CurrentState == AlertCurrentState.Alerted));

    public static Func<IUserEntity?> DefaultRecipient = () => null;

    public static Dictionary<AlertTypeSymbol, AlertTypeOptions> SystemAlertTypes = new Dictionary<AlertTypeSymbol, AlertTypeOptions>();

    public static string? GetText(this AlertTypeSymbol? alertType)
    {
        if (alertType == null)
            return null;

        var options = SystemAlertTypes.GetOrThrow(alertType);

        return options.GetText?.Invoke();
    }

    public static bool Started = false;

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!, null!)));
    }


    public static void Start(SchemaBuilder sb, params Type[] registerExpressionsFor)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<AlertEntity>()
            .WithQuery(() => a => new
            {
                Entity = a,
                a.Id,
                a.AlertDate,
                a.AlertType,
                a.State,
                a.Title,
                Text = a.Text!.Etc(100),
                a.Target,
                a.LinkTarget,
                a.Recipient,
                a.CreationDate,
                a.CreatedBy,
                a.AttendedDate,
                a.AttendedBy,
            });

        AlertGraph.Register();

        As.ReplaceExpression((AlertEntity a) => a.Text, a => a.TextField.HasText() ? a.TextField : a.AlertType.GetText());

        Schema.Current.EntityEvents<AlertEntity>().Retrieved += (a, ctx) =>
        {
            a.TextFromAlertType = a.AlertType?.GetText();
        };

        sb.Include<AlertTypeSymbol>()
            .WithSave(AlertTypeOperation.Save)
            .WithDelete(AlertTypeOperation.Delete)
            .WithQuery(() => t => new
            {
                Entity = t,
                t.Id,
                t.Name,
                t.Key,
            });

        SemiSymbolLogic<AlertTypeSymbol>.Start(sb, () => SystemAlertTypes.Keys);
        sb.Schema.EntityEvents<TypeEntity>().PreDeleteSqlSync += Type_PreDeleteSqlSync;
        sb.Schema.EntityEvents<AlertTypeSymbol>().PreDeleteSqlSync += AlertType_PreDeleteSqlSync;

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

        if (sb.WebServerBuilder != null)
            AlertsServer.Start(sb.WebServerBuilder);
    }

    static SqlPreCommand? AlertType_PreDeleteSqlSync(AlertTypeSymbol alertType)
    {
        if (Administrator.ExistsTable<AlertEntity>() && Database.Query<AlertEntity>().Any(a => a.AlertType.Is(alertType)))
        {
            var table = Schema.Current.Table<AlertEntity>();
            var column = (IColumn)(FieldReference)Schema.Current.Field((AlertEntity a) => a.AlertType);
            return Administrator.DeleteWhereScript(table, column, alertType.Id);
        }

        return null;
    }

    static SqlPreCommand? Type_PreDeleteSqlSync(TypeEntity type)
    {
        return SqlPreCommand.Combine(Spacing.Simple,
            Type_PreDeleteSqlSync(type, a => a.Target),
            Type_PreDeleteSqlSync(type, a => a.LinkTarget),
            Type_PreDeleteSqlSync(type, a => a.GroupTarget)
        );
    }

    static SqlPreCommand? Type_PreDeleteSqlSync(TypeEntity type, Expression<Func<AlertEntity, Lite<Entity>?>> ibaField)
    {
        if (Administrator.ExistsTable<AlertEntity>() && Database.Query<AlertEntity>().Any(a => ibaField.Evaluate(a)!.EntityType.ToTypeEntity().Is(type)))
        {
            var table = Schema.Current.Table<AlertEntity>();
            var column = (IColumn)((FieldImplementedByAll)Schema.Current.Field(ibaField)).TypeColumn;
            if (SafeConsole.Ask($"Delete {table.Name} with {column.Name} of type {type}?"))
            { 
                return Administrator.DeleteWhereScript(table, column, type.Id);
            }
        }

        return null;
    }

    public static void RegisterAlertNotificationMail(SchemaBuilder sb)
    {
        EmailModelLogic.RegisterEmailModel<AlertNotificationMail>(() => new EmailTemplateEntity
        {
            Messages = CultureInfoLogic.ForEachCulture(culture => new EmailTemplateMessageEmbedded(culture)
            {
                Text = $@"
<p>{AlertMessage.Hi0.NiceToString("@[m:Entity]")}</p>
<p>{AlertMessage.YouHaveSomePendingAlerts.NiceToString()}</p>
<ul>
@foreach[m:Alerts] as $a
<li>
    <strong>@[$a.Title]:</strong><br/>
    @[m:TextFormatted]<br/>
    <small>@[$a.AlertDate] @[$a.CreatedBy]</small>
</li>
 @endforeach
</ul>
<p>{AlertMessage.PleaseVisit0.NiceToString(@"<a href=""@[g:UrlLeft]"">@[g:UrlLeft]</a>")}</p>",
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

        sb.Schema.Settings.AssertImplementedBy((ScheduledTaskEntity a) => a.Task, typeof(SendNotificationEmailTaskEntity));
        sb.Schema.Settings.AssertImplementedBy((ScheduledTaskLogEntity a) => a.Task, typeof(SendNotificationEmailTaskEntity));

        SchedulerLogic.ExecuteTask.Register((SendNotificationEmailTaskEntity task, ScheduledTaskContext ctx) =>
        {
            var max = Clock.Now.AddMinutes(-task.SendNotificationsOlderThan);
            var min = task.IgnoreNotificationsOlderThan == null ? (DateTime?)null : Clock.Now.AddDays(-task.IgnoreNotificationsOlderThan.Value);

            var query = Database.Query<AlertEntity>()
            .Where(a => a.State == AlertState.Saved && a.EmailNotificationsSent == false && a.AvoidSendMail == false && a.Recipient != null && (min == null || min < a.AlertDate) && a.AlertDate < max)
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
                a.Mixin<EmailMessagePackageMixin>().Package = emailPackage.ToLite();
            });

            emails.BulkInsertQueryIds(a => a.Target!);

            query.UnsafeUpdate().Set(a => a.EmailNotificationsSent, a => true).Execute();

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

        static Regex LinkPlaceholder = new Regex(@"\[(?<prop>(\w|\d|\.)+)(\:(?<text>.+))?\](\((?<url>.+)\))?");

        public static HtmlString? TextFormatted(TemplateParameters tp)
        {
            if (!tp.RuntimeVariables.TryGetValue("$a", out object? alertObject))
                return null;

            return GetAlertText((AlertEntity)alertObject!);
        }

        public static HtmlString GetAlertText(AlertEntity alert)
        {
            var text = alert.Text ?? "";

            var newText = LinkPlaceholder.SplitAfter(text).Select(pair =>
            {
                try
                {
                    var m = pair.match;
                    if (m == null)
                        return ReplacePlaceHolders(pair.after, alert);

                    var propEx = m.Groups["prop"].Value;

                    var prop = GetPropertyValue(alert, propEx);

                    var lite = prop is Entity e ? e.ToLite() :
                                prop is Lite<Entity> l ? l : null;

                    var url = ReplacePlaceHolders(m.Groups["url"].Value.DefaultToNull(), alert)?.Let(url => url.StartsWith("~") ? (EmailLogic.Configuration.UrlLeft + url.After("~")) : url) ??
                    (lite != null ? EntityUrl(lite) : "#");

                    var text = ReplacePlaceHolders(m.Groups["text"].Value.DefaultToNull(), alert) ?? (lite?.ToString());

                    return @$"<a href=""{url}"">{text}</a>" + ReplacePlaceHolders(pair.after, alert);
                }
                catch (Exception e)
                {
                    return ("<span style='color:red'>ERROR: " + e.Message + "</span>") + pair.match?.Value + pair.after;
                }
            }).ToString("");

            if (text != newText)
                return new HtmlString(newText);

            if (alert.Target != null)
                return new HtmlString(@$"{text}<br/><a href=""{EntityUrl(alert.Target)}"">{alert.Target}</a>");

            return new HtmlString(text);
        }


        private static string EntityUrl(Lite<Entity> lite)
        {
            return $"{EmailLogic.Configuration.UrlLeft}/view/{TypeLogic.GetCleanName(lite.EntityType)}/{lite.Id}";
        }

        static Regex TextPlaceHolder = new Regex(@"({(?<prop>(\w|\d|\.)+)})");
        static Regex NumericPlaceholder = new Regex(@"^[ \d]+$");
        private static string? ReplacePlaceHolders(string? value, AlertEntity alert)
        {
            if (value == null)
                return null;

            return TextPlaceHolder.Replace(value, g =>
            {
                var prop = g.Groups["prop"].Value;
                if (NumericPlaceholder.IsMatch(prop))
                    return alert.TextArguments?.Split("\n###\n").ElementAtOrDefault(int.Parse(prop)) ?? "";
                
                return GetPropertyValue(alert, prop)?.ToString()!;
            });
        }

        private static object? GetPropertyValue(AlertEntity alert, string expresion)
        {
            var parts = expresion.SplitNoEmpty('.');

            var result = SimpleMemberEvaluator.EvaluateExpression(alert, parts);

            if (result is Result<object?>.Error e)
                throw new InvalidOperationException(e.ErrorText);

            if (result is Result<object?>.Success s)
                return s.Value;

            throw new UnexpectedValueException(result);
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

    public static void RegisterAlertType(AlertTypeSymbol alertType, Enum localizableTextMessage) => RegisterAlertType(alertType, new AlertTypeOptions { GetText = () => localizableTextMessage.NiceToString() });
    public static void RegisterAlertType(AlertTypeSymbol alertType, AlertTypeOptions? options = null)
    {
        if (!alertType.Key.HasText())
            throw new InvalidOperationException("alertType must have a key, use MakeSymbol method after the constructor when declaring it");

        SystemAlertTypes.Add(alertType, options ?? new AlertTypeOptions());
    }

    public static AlertEntity? CreateAlert(this IEntity entity, AlertTypeSymbol alertType, string? text = null, string?[]? textArguments = null, DateTime? alertDate = null, 
        Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null, Lite<Entity>? linkTarget = null, Lite<Entity>? groupTarget = null, bool avoidSendMail = false)
    {
        return CreateAlert(entity.ToLiteFat(), alertType, text, textArguments, alertDate, createdBy, title, recipient, linkTarget, groupTarget, avoidSendMail);
    }

    static IDisposable AllowSaveAlerts() => TypeAuthLogic.OverrideTypeAllowed<AlertEntity>(tac => WithConditions<TypeAllowed>.Simple(TypeAllowed.Write));

    public static AlertEntity? CreateAlert(this Lite<IEntity> entity, AlertTypeSymbol alertType, string? text = null, string?[]? textArguments = null, DateTime? alertDate = null, 
        Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null, Lite<Entity>? linkTarget = null, Lite<Entity>? groupTarget = null, bool avoidSendMail = false)
    {
        if (Started == false)
            return null;

        using (AllowSaveAlerts())
        using (OperationLogic.AllowSave<AlertEntity>())
        {
            var result = new AlertEntity
            {
                AlertDate = alertDate ?? Clock.Now,
                CreatedBy = createdBy ?? UserHolder.Current?.User,
                TitleField = title,
                TextArguments = textArguments?.ToString("\n###\n"),
                TextField = text,
                Target = (Lite<Entity>)entity,
                LinkTarget = linkTarget,
                GroupTarget = groupTarget,
                AlertType = alertType,
                Recipient = recipient,
                AvoidSendMail = avoidSendMail,
                State = AlertState.Saved,
            }.Save();

            return result;
        }
    }

    public static int? UnsafeInsertAlerts(IQueryable<(Lite<IUserEntity>? recipient, Lite<Entity>? target)> query, AlertTypeSymbol alertType, string? text = null, 
        string?[]? textArguments = null, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<Entity>? linkTarget = null, Lite<Entity>? groupTarget = null, bool avoidSendMail = false)
    {
        if (Started == false)
            return null;

        using (AllowSaveAlerts())
        {
            alertDate ??= Clock.Now;
            createdBy ??= UserHolder.Current?.User;

            var txtArgumentJoined = textArguments?.ToString("\n###\n");
            return query.UnsafeInsert(tuple => new AlertEntity
            {
                AlertDate = alertDate,
                CreatedBy = createdBy,
                TitleField = title,
                TextArguments = txtArgumentJoined,
                TextField = text,
                Target = tuple.target,
                LinkTarget = linkTarget,
                GroupTarget= groupTarget,
                AlertType = alertType,
                Recipient = tuple.recipient,
                State = AlertState.Saved,
                EmailNotificationsSent = false,
                AvoidSendMail = avoidSendMail,
            }.SetReadonly(a => a.CreationDate, Clock.Now));
        }
    }


    public static AlertEntity? CreateAlertForceNew(this IEntity entity, AlertTypeSymbol alertType, string? text = null, string?[]? textArguments = null, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null, Lite<Entity>? linkTarget = null, Lite<Entity>? groupTarget = null, bool avoidSendMail = false)
    {
        return CreateAlertForceNew(entity.ToLite(), alertType, text, textArguments, alertDate, createdBy, title, recipient, linkTarget, groupTarget, avoidSendMail);
    }

    public static AlertEntity? CreateAlertForceNew(this Lite<IEntity> entity, AlertTypeSymbol alertType, string? text = null, string?[]? textArguments = null, DateTime? alertDate = null, Lite<IUserEntity>? createdBy = null, string? title = null, Lite<IUserEntity>? recipient = null, Lite<Entity>? linkTarget = null, Lite<Entity>? groupTarget = null, bool avoidSendMail = false)
    {
        if (Started == false)
            return null;

        using (var tr = Transaction.ForceNew())
        {
            var alert = entity.CreateAlert(alertType, text, textArguments, alertDate, createdBy, title, recipient, linkTarget, groupTarget, avoidSendMail);

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

    public static void AttendAllAlerts(Lite<Entity> target, AlertTypeSymbol alertType)
    {
        using (AllowSaveAlerts())
        {
            Database.Query<AlertEntity>()
                .Where(a => a.Target.Is(target) && a.AlertType.Is(alertType) && a.State == AlertState.Saved)
                .UnsafeUpdate()
                .Set(a => a.State, a => AlertState.Attended)
                .Set(a => a.AttendedDate, a => Clock.Now)
                .Set(a => a.AttendedBy, a => UserHolder.Current.User)
                .Execute();
        }
    }

    public static void AttendAllAlerts(this IQueryable<AlertEntity> alerts)
    {
        using (AuthLogic.Disable())
        {
            alerts
                 .Where(a => a.State == AlertState.Saved)
                .UnsafeUpdate()
                .Set(a => a.State, a => AlertState.Attended)
                .Set(a => a.AttendedDate, a => Clock.Now)
                .Set(a => a.AttendedBy, a => UserHolder.Current.User)
                .Execute();
        }
    }

    public static void DeleteAllAlerts(Lite<Entity> target)
    {
        using (AllowSaveAlerts())
        {
            Database.Query<AlertEntity>()
                .Where(a => a.Target.Is(target))
                .UnsafeDelete();

            Database.Query<AlertEntity>()
                .Where(a => a.LinkTarget.Is(target))
                .UnsafeDelete();
        }
    }


    public static void DeleteUnattendedAlerts(this Entity target, AlertTypeSymbol alertType, Lite<UserEntity>? recipient = null) =>
        target.ToLite().DeleteUnattendedAlerts(alertType, recipient);
    public static void DeleteUnattendedAlerts(this Lite<Entity> target, AlertTypeSymbol alertType, Lite<UserEntity>? recipient = null)
    {
        using (AllowSaveAlerts())
        {
            Database.Query<AlertEntity>()
                .Where(a => a.State == AlertState.Saved && a.Target.Is(target) && a.AlertType.Is(alertType) && (recipient == null || a.Recipient.Is(recipient)))
                .UnsafeDelete();
        }
    }

}

public class AlertTypeOptions
{
    public Func<string>? GetText; 
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
                AlertDate = Clock.Now,
                CreatedBy = UserHolder.Current.User,
                Recipient = AlertLogic.DefaultRecipient()?.ToLite(),
                TitleField = null,
                TextField = null,
                Target = a.ToLite(),
                AlertType = null
            }
        }.Register();

        new Construct(AlertOperation.Create)
        {
            ToStates = { AlertState.New },
            Construct = (_) => new AlertEntity
            {
                AlertDate = Clock.Now,
                CreatedBy = UserHolder.Current.User,
                Recipient = AlertLogic.DefaultRecipient()?.ToLite(),
                TitleField = null,
                TextField = null,
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
                a.AttendedDate = Clock.Now;
                a.AttendedBy = UserHolder.Current.User;
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
