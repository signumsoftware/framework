using System.Globalization;
using Signum.DynamicQuery.Tokens;
using Signum.Basics;
using Signum.Engine.Sync;
using Signum.Basics;
using Signum.Templating;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.Queries;
using Signum.API;
using Signum.Authorization;
using System.Collections.Frozen;

namespace Signum.Mailing.Templates;

public static class EmailTemplateLogic
{
    public static bool AvoidSynchronizeTokens = false;
    public static bool AvoidSynchronizeDefaultTemplates = true;

    public static Func<Entity?, CultureInfo>? GetCultureInfo;

    public static EmailTemplateMessageEmbedded? GetCultureMessage(this EmailTemplateEntity template, CultureInfo ci)
    {
        return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo().Equals(ci));
    }

    [AutoExpressionField]
    public static IQueryable<EmailTemplateEntity> EmailTemplates(this EmailModelEntity se) =>
        As.Expression(() => Database.Query<EmailTemplateEntity>().Where(et => et.Model.Is(se)));

    public static ResetLazy<FrozenDictionary<Lite<EmailTemplateEntity>, EmailTemplateEntity>> EmailTemplatesLazy = null!;
    public static ResetLazy<FrozenDictionary<object, List<EmailTemplateEntity>>> TemplatesByQueryName = null!;


    public static Polymorphic<Action<IAttachmentGeneratorEntity, FillAttachmentTokenContext>> FillAttachmentTokens =
       new Polymorphic<Action<IAttachmentGeneratorEntity, FillAttachmentTokenContext>>();

    public class FillAttachmentTokenContext
    {
        public QueryDescription QueryDescription;
        public List<QueryToken> QueryTokens;
        public Type? ModelType;

        public FillAttachmentTokenContext(QueryDescription queryDescription, List<QueryToken> queryTokens)
        {
            QueryDescription = queryDescription;
            QueryTokens = queryTokens;
        }
    }

    public static Polymorphic<Func<IAttachmentGeneratorEntity, GenerateAttachmentContext, List<EmailAttachmentEmbedded>>> GenerateAttachment =
        new Polymorphic<Func<IAttachmentGeneratorEntity, GenerateAttachmentContext, List<EmailAttachmentEmbedded>>>();
    

    public class GenerateAttachmentContext
    {
        public QueryContext? QueryContext; 
        public EmailTemplateEntity Template;
        public CultureInfo Culture;
        public Type? ModelType;
        public IEntity? Entity;
        public IEmailModel? Model;

        public GenerateAttachmentContext(EmailTemplateEntity template, CultureInfo culture)
        {
            Template = template;
            Culture = culture;
        }
    }


    public static Func<EmailTemplateEntity, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity?>? GetSmtpConfiguration;

    public static void Start(SchemaBuilder sb, Func<EmailTemplateEntity, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity?>? getSmtpConfiguration)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        CultureInfoLogic.AssertStarted(sb);

        GetSmtpConfiguration = getSmtpConfiguration;

        sb.Include<EmailTemplateEntity>()
            .WithQuery(() => t => new
            {
                Entity = t,
                t.Id,
                t.Name,
                t.Query,
                t.Model,
            });


            

        EmailTemplatesLazy = sb.GlobalLazy(() =>
        Database.Query<EmailTemplateEntity>().ToFrozenDictionaryEx(et => et.ToLite())
        , new InvalidateWith(typeof(EmailTemplateEntity)));

        TemplatesByQueryName = sb.GlobalLazy(() =>
        {
            return EmailTemplatesLazy.Value.Values.Where(a => a.Query != null).SelectCatch(et => KeyValuePair.Create(et.Query!.ToQueryName(), et)).GroupToDictionary().ToFrozenDictionary();
        }, new InvalidateWith(typeof(EmailTemplateEntity)));

        EmailModelLogic.Start(sb);
        EmailMasterTemplateLogic.Start(sb);

        sb.Schema.EntityEvents<EmailTemplateEntity>().PreSaving += new PreSavingEventHandler<EmailTemplateEntity>(EmailTemplate_PreSaving);
        sb.Schema.EntityEvents<EmailTemplateEntity>().Retrieved += EmailTemplateLogic_Retrieved;

        Validator.OverridePropertyValidator((EmailTemplateMessageEmbedded m) => m.Text).StaticPropertyValidation +=
            EmailTemplateMessageText_StaticPropertyValidation;

        Validator.OverridePropertyValidator((EmailTemplateMessageEmbedded m) => m.Subject).StaticPropertyValidation +=
            EmailTemplateMessageSubject_StaticPropertyValidation;

        EmailTemplateGraph.Register();

        GlobalValueProvider.RegisterGlobalVariable("UrlLeft", _ => EmailLogic.Configuration.UrlLeft);
        GlobalValueProvider.RegisterGlobalVariable("Now", _ => Clock.Now);
        GlobalValueProvider.RegisterGlobalVariable("Today", _ => Clock.Now.Date, "d");
        GlobalValueProvider.RegisterGlobalVariable("CurrentUser", _ => UserEntity.Current.Retrieve(), "d");

        sb.Schema.Synchronizing += Schema_Synchronizing_Tokens;
        sb.Schema.Synchronizing += Schema_Synchronizing_DefaultTemplates;

        sb.Schema.EntityEvents<EmailModelEntity>().PreDeleteSqlSync += EmailTemplateLogic_PreDeleteSqlSync;

        Validator.PropertyValidator<EmailTemplateEntity>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
        {
            var dc = EmailLogic.Configuration.DefaultCulture;

            if (!et.Messages.Any(m => m.CultureInfo != null && dc.Name.StartsWith(m.CultureInfo.Name)))
                return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(CultureInfoLogic.EntityToCultureInfo.Value.Keys.Where(c => dc.Name.StartsWith(c.Name)).CommaOr(a => a.EnglishName));

            return null;
        };
    }

    static SqlPreCommand? EmailTemplateLogic_PreDeleteSqlSync(EmailModelEntity entityModel)
    {
    retry:
        var result = new ConsoleSwitch<string, Func<SqlPreCommand>>($"EmailModel {entityModel} has been removed... What you want to do?")
        {
            {
                "mt",
                () => SqlPreCommand.Combine(Spacing.Simple,
                    Administrator.UnsafeDeletePreCommand(Database.Query<EmailMessageEntity>().Where(em => em.Template!.Entity.Model.Is(entityModel))),
                    Administrator.UnsafeDeletePreCommand(Database.Query<EmailTemplateEntity>().Where(et => et.Model.Is(entityModel)))
                )!,
                "Delete Messages and Templates"
            },
            { "t",
                () => SqlPreCommand.Combine(Spacing.Simple,
                    Administrator.UnsafeUpdatePartPreCommand(Database.Query<EmailMessageEntity>().Where(em => em.Template!.Entity.Model.Is(entityModel)).UnsafeUpdate().Set(em  => em.Template, em => null)),
                    Administrator.UnsafeDeletePreCommand(Database.Query<EmailTemplateEntity>().Where(et => et.Model.Is(entityModel)))
                )!,
                "Delete Templates only, Update Messages"

            },
            {
                "n",
                () => Administrator.UnsafeUpdatePartPreCommand(Database.Query<EmailTemplateEntity>().Where(et => et.Model.Is(entityModel)).UnsafeUpdate().Set(a => a.Model, a => null)),
                "Nothing, just Update Templates"
            },
        }.Choose();

        if (result != null)
            return result();

        goto retry;
    }

    public static EmailTemplateEntity ParseData(this EmailTemplateEntity emailTemplate)
    {
        if (!emailTemplate.IsNew || emailTemplate.queryName == null)
            throw new InvalidOperationException("emailTemplate should be new and have queryName");

        emailTemplate.Query = QueryLogic.GetQueryEntity(emailTemplate.queryName);

        QueryDescription description = QueryLogic.Queries.QueryDescription(emailTemplate.queryName);

        emailTemplate.ParseData(description);

        return emailTemplate;
    }

    static void EmailTemplateLogic_Retrieved(EmailTemplateEntity emailTemplate, PostRetrievingContext ctx)
    {
        if (emailTemplate.Query != null)
        {
            using (emailTemplate.DisableAuthorization ? ExecutionMode.Global() : null)
            {
                object queryName = emailTemplate.Query!.ToQueryName();
                QueryDescription? description = QueryLogic.Queries.QueryDescription(queryName);

                emailTemplate.ParseData(description);
            }
        }
    }

    static string? EmailTemplateMessageText_StaticPropertyValidation(EmailTemplateMessageEmbedded message, PropertyInfo pi)
    {
        if (message.TextParsedNode as TextTemplateParser.BlockNode == null)
        {
            try
            {
                message.TextParsedNode = ParseTemplate(message.GetParentEntity<EmailTemplateEntity>(), message.Text, out string errorMessage);
                return errorMessage.DefaultToNull();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        return null;
    }

    static string? EmailTemplateMessageSubject_StaticPropertyValidation(EmailTemplateMessageEmbedded message, PropertyInfo pi)
    {
        if (message.SubjectParsedNode as TextTemplateParser.BlockNode == null)
        {
            try
            {
                message.SubjectParsedNode = ParseTemplate(message.GetParentEntity<EmailTemplateEntity>(), message.Subject, out string errorMessage);
                return errorMessage.DefaultToNull();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        return null;
    }

    public static TextTemplateParser.BlockNode ParseTemplate(EmailTemplateEntity template, string? text, out string errorMessage)
    {
        using (template.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            object? queryName = template.Query?.ToQueryName();
            QueryDescription? qd = queryName == null ? null : QueryLogic.Queries.QueryDescription(queryName);

            return TextTemplateParser.TryParse(text, qd, template.Model?.ToType(), out errorMessage);
        }
    }

    static void EmailTemplate_PreSaving(EmailTemplateEntity template, PreSavingContext ctx)
    {
        using (template.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            var queryName = template.Query?.ToQueryName();
            QueryDescription? qd = queryName == null ? null : QueryLogic.Queries.QueryDescription(queryName);

            foreach (var message in template.Messages)
            {
                message.Text = TextTemplateParser.Parse(message.Text, qd, template.Model?.ToType()).ToString();
                message.Subject = TextTemplateParser.Parse(message.Subject, qd, template.Model?.ToType()).ToString();
            }
        }
    }

    public static Func<Lite<EmailTemplateEntity>, EmailTemplateEntity> GetEmailTemplate = liteTemplate => EmailTemplatesLazy.Value.GetOrThrow(liteTemplate, "Email template {0} not in cache".FormatWith(liteTemplate));
    
    public static IEnumerable<EmailMessageEntity> CreateEmailMessage(this Lite<EmailTemplateEntity> liteTemplate, ModifiableEntity? modifiableEntity = null, IEmailModel? model = null, CultureInfo? cultureInfo = null)
    {
        EmailTemplateEntity template = GetEmailTemplate(liteTemplate);

        return CreateEmailMessage(template, modifiableEntity, ref model, cultureInfo);
    }

    public static IEnumerable<EmailMessageEntity> CreateEmailMessage(this EmailTemplateEntity template, ModifiableEntity? modifiableEntity = null, IEmailModel? model = null, CultureInfo? cultureInfo = null)
    {
        return CreateEmailMessage(template, modifiableEntity, ref model, cultureInfo);
    }

    private static IEnumerable<EmailMessageEntity> CreateEmailMessage(EmailTemplateEntity template, ModifiableEntity? modifiableEntity, ref IEmailModel? model, CultureInfo? cultureInfo = null)
    {
        Entity? entity = null;
        if (template.Model != null)
        {
            if (model == null)
                model = EmailModelLogic.CreateModel(template.Model, modifiableEntity);
            else if (template.Model.ToType() != model.GetType())
                throw new ArgumentException("model should be a {0} instead of {1}".FormatWith(template.Model.FullClassName, model.GetType().FullName));
        }
        else
        {
            if (modifiableEntity != null)
                entity = modifiableEntity as Entity ?? throw new InvalidOperationException("Model should be an Entity");
        }

        using (template.DisableAuthorization ? ExecutionMode.Global() : null)
        {
            var emailBuilder = new EmailMessageBuilder(template, entity, model, cultureInfo);
            return emailBuilder.CreateEmailMessageInternal().ToList();
        }
    }

    class EmailTemplateGraph : Graph<EmailTemplateEntity>
    {
        static bool registered;
        public static bool Registered { get { return registered; } }

        public static void Register()
        {
            new Construct(EmailTemplateOperation.Create)
            {
                Construct = _ => new EmailTemplateEntity
                {
                    MasterTemplate = EmailMasterTemplateLogic.GetDefaultMasterTemplate(),
                }
            }.Register();

            new ConstructFrom<EmailTemplateEntity>(EmailTemplateOperation.Clone)
            {
                Construct = (e, _) =>
                {
                    return new EmailTemplateEntity()
                    {
                        Name = $"{e.Name} (Cloned)",
                        MasterTemplate = e.MasterTemplate,
                        Applicable = e.Applicable == null ? null : new TemplateApplicableEval() { Script = e.Applicable.Script },
                        DisableAuthorization = e.DisableAuthorization,
                        EditableMessage = e.EditableMessage,
                        GroupResults = e.GroupResults,
                        MessageFormat = e.MessageFormat,
                        From = e.From?.Clone(),
                        Recipients = e.Recipients.Select(r => r.Clone()).ToMList(),
                        Query = e.Query,
                        Model = e.Model,
                        Filters = e.Filters.Select(f => f.Clone()).ToMList(),
                        Orders = e.Orders.Select(o => o.Clone()).ToMList(),
                        Messages = e.Messages.Select(m => m.Clone()).ToMList(),
                        Attachments = e.Attachments.Select(a => a.Clone()).ToMList(),
                    };
                }
            }.Register();

            new Execute(EmailTemplateOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (t, _) => { }
            }.Register();

            new Delete(EmailTemplateOperation.Delete)
            {
                Delete = (t, _) =>
                {
                    var attachments = t.Attachments.Select(a => a.ToLite()).ToList();

                    t.Delete();
                    attachments.ForEach(at => at.Delete());

                }
            }.Register();

            registered = true;
        }
    }

    static SqlPreCommand? Schema_Synchronizing_Tokens(Replacements replacements)
    {
        if (AvoidSynchronizeTokens)
            return null;

        QueryLogic.AssertLoaded();
        TypeLogic.AssertLoaded();

        var dc = EmailLogic.Configuration.DefaultCulture; // To avoid many exceptions

        StringDistance sd = new StringDistance();

        var emailTemplates = Database.Query<EmailTemplateEntity>().ToList();

        var table = Schema.Current.Table(typeof(EmailTemplateEntity));

        EmailModelLogic.EntityToType.Load(); //To avoid N exceptions

        SqlPreCommand? cmd = emailTemplates.Select(et => ProcessEmailTemplate(replacements, table, et, sd)).Combine(Spacing.Double);

        return cmd;
    }

    internal static SqlPreCommand? ProcessEmailTemplate(Replacements replacements, Table table, EmailTemplateEntity et, StringDistance sd)
    {
        Console.Write(".");
        try
        {
            var queryName = et.Query?.ToQueryName();

            QueryDescription? qd = queryName == null ? null : QueryLogic.Queries.QueryDescription(queryName);

            SqlPreCommand DeleteTemplate()
            {
                return table.DeleteSqlSync(et, e => e.Guid == et.Guid).TransactionBlock($"EmailTemplate Guid = {et.Guid}")!;
            }

            SqlPreCommand? RegenerateTemplate()
            {
                var newTemplate = EmailModelLogic.CreateDefaultTemplateInternal(et.Model!);

                newTemplate.SetId(et.IdOrNull);
                newTemplate.SetIsNew(false);
                newTemplate.Ticks = et.Ticks;

                using (replacements.WithReplacedDatabaseName())
                    return table.UpdateSqlSync(newTemplate, e => e.Name == newTemplate.Name, includeCollections: true, comment: "EmailTemplate Regenerated: " + et.Name);
            }

            using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name)))
            using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + (et.Query?.Key ?? " -- "))))
            {
                if (qd != null)
                {
                    if (et.From != null && et.From.Token != null)
                    {
                        QueryTokenEmbedded token = et.From.Token;
                        switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " From", allowRemoveToken: false, allowReCreate: et.Model != null))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return DeleteTemplate();
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: et.From.Token = token; break;
                            case FixTokenResult.RegenerateEntity: return RegenerateTemplate();
                            default: break;
                        }
                    }

                    if (et.Recipients.Any(a => a.Token != null))
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Recipients:")))
                        {
                            foreach (var item in et.Recipients.Where(a => a.Token != null).ToList())
                            {
                                QueryTokenEmbedded token = item.Token!;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Recipient", allowRemoveToken: false, allowReCreate: et.Model != null))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return DeleteTemplate();
                                    case FixTokenResult.RemoveToken: et.Recipients.Remove(item); break;
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix: item.Token = token; break;
                                    case FixTokenResult.RegenerateEntity: return RegenerateTemplate();
                                    default: break;
                                }
                            }
                        }
                    }

                    if (et.Filters.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Filters:")))
                        {
                            foreach (var item in et.Filters.ToList())
                            {
                                QueryTokenEmbedded token = item.Token!;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Filters", allowRemoveToken: false, allowReCreate: et.Model != null))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return DeleteTemplate();
                                    case FixTokenResult.RemoveToken: et.Filters.Remove(item); break;
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix: item.Token = token; break;
                                    case FixTokenResult.RegenerateEntity: return RegenerateTemplate();
                                    default: break;
                                }
                            }
                        }
                    }

                    if (et.Orders.Any())
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Orders:")))
                        {
                            foreach (var item in et.Orders.ToList())
                            {
                                QueryTokenEmbedded token = item.Token!;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Orders", allowRemoveToken: false, allowReCreate: et.Model != null))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return DeleteTemplate();
                                    case FixTokenResult.RemoveToken: et.Orders.Remove(item); break;
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix: item.Token = token; break;
                                    case FixTokenResult.RegenerateEntity: return RegenerateTemplate();
                                    default: break;
                                }
                            }
                        }
                    }
                }

                try
                {

                    foreach (var item in et.Messages)
                    {
                        var sc = new TemplateSynchronizationContext(et, replacements, sd, qd, et.Model?.ToType());

                        item.Subject = TextTemplateParser.Synchronize(item.Subject, sc);
                        item.Text = TextTemplateParser.Synchronize(item.Text, sc);
                    }

                    using (replacements.WithReplacedDatabaseName())
                        return table.UpdateSqlSync(et, e => e.Guid == et.Guid && e.Ticks == et.Ticks, includeCollections: true, comment: "EmailTemplate: " + et.Name)?.TransactionBlock($"EmailTemplate Guid = {et.Guid} Ticks = {et.Ticks} ({et})");
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return table.DeleteSqlSync(et, e => e.Name == et.Name);

                    if (ex.Result == FixTokenResult.RegenerateEntity)
                        return RegenerateTemplate();

                    throw new UnexpectedValueException(ex.Result);
                }
            }
        }
        catch (Exception e)
        {
            return new SqlPreCommandSimple("-- Exception on {0}. {1}\n{2}".FormatWith(et.BaseToString(), e.GetType().Name, e.Message.Indent(2, '-')));
        }
    }

    static SqlPreCommand? Schema_Synchronizing_DefaultTemplates(Replacements replacements)
    {
        if (AvoidSynchronizeDefaultTemplates)
            return null;

        var dc = EmailLogic.Configuration.DefaultCulture; // To avoid many exceptions

        var table = Schema.Current.Table(typeof(EmailTemplateEntity));

        var emailModels = Database.Query<EmailModelEntity>().Where(se => !se.EmailTemplates().Any()).ToList();

        string cis = Database.Query<CultureInfoEntity>().Select(a => a.Name).ToString(", ").Etc(60);

        if (!emailModels.Any())
            return null;

        if (!replacements.Interactive || !SafeConsole.Ask("{0}\n have no EmailTemplates. Create in {1}?".FormatWith(emailModels.ToString("\n"), cis.DefaultText("No CultureInfos registered!"))))
            return null;

        using (replacements.WithReplacedDatabaseName())
        {
            var cmd = emailModels.Select(se =>
            {
                try
                {
                    return table.InsertSqlSync(EmailModelLogic.CreateDefaultTemplateInternal(se), includeCollections: true);
                }
                catch (Exception e)
                {
                    return new SqlPreCommandSimple("Exception on SystemEmail {0}: {1}".FormatWith(se, e.Message));
                }
            }).Combine(Spacing.Double);

            if (cmd != null)
                return SqlPreCommand.Combine(Spacing.Double, new SqlPreCommandSimple("DECLARE @parentId INT"), cmd);

            return cmd;
        }
    }

    public static void GenerateDefaultTemplates()
    {
        var systemEmails = Database.Query<EmailModelEntity>().Where(se => !se.EmailTemplates().Any()).ToList();

        List<string> exceptions = new List<string>();

        foreach (var se in systemEmails)
        {
            try
            {
                EmailModelLogic.CreateDefaultTemplateInternal(se).Save();
            }
            catch (Exception ex)
            {
                exceptions.Add("{0} in {1}:\n{2}".FormatWith(ex.GetType().Name, se.FullClassName, ex.Message.Indent(4)));
            }
        }

        if (exceptions.Any())
            throw new Exception(exceptions.ToString("\n\n"));
    }

    public static bool Regenerate(EmailTemplateEntity et)
    {
        var newTemplate = EmailModelLogic.CreateDefaultTemplateInternal(et.Model!);
        if (newTemplate == null)
            return false;

        newTemplate.SetId(et.IdOrNull);
        newTemplate.SetIsNew(false);
        newTemplate.Ticks = et.Ticks;
        newTemplate.Save();
        return true;
    }


    public static Dictionary<Type, EmailTemplateVisibleOn> VisibleOnDictionary = new Dictionary<Type, EmailTemplateVisibleOn>()
    {
        { typeof(MultiEntityModel), EmailTemplateVisibleOn.Single | EmailTemplateVisibleOn.Multiple},
        { typeof(QueryModel), EmailTemplateVisibleOn.Single | EmailTemplateVisibleOn.Multiple| EmailTemplateVisibleOn.Query},
    };

    public static bool IsVisible(EmailTemplateEntity et, EmailTemplateVisibleOn visibleOn)
    {
        if (et.Model == null)
            return visibleOn == EmailTemplateVisibleOn.Single;

        if (EmailModelLogic.HasDefaultTemplateConstructor(et.Model))
            return false;

        var entityType = EmailModelLogic.GetEntityType(et.Model.ToType());

        if (entityType.IsEntity())
            return visibleOn == EmailTemplateVisibleOn.Single;

        var should = VisibleOnDictionary.TryGet(entityType, EmailTemplateVisibleOn.Single);

        return (should & visibleOn) != 0;
    }


    public static List<Lite<EmailTemplateEntity>> GetApplicableEmailTemplates(object queryName, Entity? entity, EmailTemplateVisibleOn visibleOn)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<EmailTemplateEntity>(userInterface: false);
        return TemplatesByQueryName.Value.TryGetC(queryName).EmptyIfNull()
            .Where(a => isAllowed(a) && IsVisible(a, visibleOn))
            .Where(a => a.IsApplicable(entity))
            .Select(a => a.ToLite())
            .ToList();
    }
}
