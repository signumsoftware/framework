using System.Net.Mail;
using Signum.Entities.Mailing;
using Signum.Entities.Processes;
using Signum.Engine.Authorization;
using Signum.Utilities.Reflection;
using System.IO;
using Signum.Engine.Files;
using Microsoft.AspNetCore.StaticFiles;
using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Engine.Mailing.Senders;
using Signum.Utilities;

namespace Signum.Engine.Mailing;

public static class EmailLogic
{
    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> EmailMessages(this EmailPackageEntity e) =>
        As.Expression(() => Database.Query<EmailMessageEntity>().Where(a => a.Package.Is(e)));

    static Func<EmailConfigurationEmbedded> getConfiguration = null!;
    public static EmailConfigurationEmbedded Configuration
    {
        get { return getConfiguration(); }
    }

    static Func<EmailTemplateEntity?, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity> getEmailSenderConfiguration = null!;

    public static Polymorphic<Func<EmailServiceEntity, EmailSenderConfigurationEntity, BaseEmailSender>> EmailSenders = new ();       
    public static BaseEmailSender GetEmailSender(EmailMessageEntity email)
    {
        var template = email.Template?.Try(t => EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow(t));
        var config = getEmailSenderConfiguration(template, email.Target, email);
        return EmailSenders.Invoke(config.Service, config);
    }

    internal static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmailLogic.Start(null!, null!, null!, null)));
    }

    public static void Start(
        SchemaBuilder sb,
        Func<EmailConfigurationEmbedded> getConfiguration,
        Func<EmailTemplateEntity?, Lite<Entity>?, EmailMessageEntity?, EmailSenderConfigurationEntity> getEmailSenderConfiguration,
        IFileTypeAlgorithm? attachment = null)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            FilePathEmbeddedLogic.AssertStarted(sb);
            CultureInfoLogic.AssertStarted(sb);
            EmailLogic.getConfiguration = getConfiguration;
            EmailTemplateLogic.Start(sb, getEmailSenderConfiguration);
            EmailSenderConfigurationLogic.Start(sb);
            if (attachment != null)
                FileTypeLogic.Register(EmailFileType.Attachment, attachment);

            Schema.Current.WhenIncluded<ProcessEntity>(() => EmailPackageLogic.Start(sb));

            sb.Include<EmailMessageEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.State,
                    e.Subject,
                    e.Template,
                    e.Sent,
                    e.Target,
                    e.Package,
                    e.SentBy,
                    e.Exception,
                });

            PermissionAuthLogic.RegisterPermissions(AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel);

            EmailLogic.getEmailSenderConfiguration = getEmailSenderConfiguration;

            EmailSenders.Register((SmtpEmailServiceEntity s, EmailSenderConfigurationEntity c) => new SmtpSender(c, s));
            EmailSenders.Register((MicrosoftGraphEmailServiceEntity s, EmailSenderConfigurationEntity c) => new MicrosoftGraphSender(c, s));
            EmailSenders.Register((ExchangeWebServiceEmailServiceEntity s, EmailSenderConfigurationEntity c) => new ExchangeWebServiceSender(c, s));

            EmailGraph.Register();

            QueryLogic.Expressions.Register((EmailPackageEntity a) => a.EmailMessages(), () => typeof(EmailMessageEntity).NicePluralName());

            ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            ExceptionLogic.DeleteLogs += ExceptionLogic_DeletePackages;
        }
    }

    public static void ExceptionLogic_DeletePackages(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        Database.Query<EmailPackageEntity>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack) && !pack.EmailMessages().Any())
            .UnsafeDeleteChunksLog(parameters, sb, token);
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(DateTime? dateLimit, bool withExceptions)
        {
            if (dateLimit == null)
                return;

            var query = Database.Query<EmailMessageEntity>().Where(o => o.CreationDate < dateLimit.Value);

            if (withExceptions)
                query = query.Where(a => a.Exception != null);

            query.UnsafeDeleteChunksLog(parameters, sb, token);
        }

        Remove(parameters.GetDateLimitDelete(typeof(EmailMessageEntity).ToTypeEntity()), withExceptions: false);
        Remove(parameters.GetDateLimitDeleteWithExceptions(typeof(EmailMessageEntity).ToTypeEntity()), withExceptions: true);
    }

    public static HashSet<Type> GetAllTypes()
    {
        if (Schema.Current.IsAllowed(typeof(EmailMessageEntity), true) != null)
            return new HashSet<Type>();

        var field = Schema.Current.Field((EmailMessageEntity em) => em.Target);

        if (field is FieldImplementedBy ib)
            return ib.ImplementationColumns.Keys.ToHashSet();

        //Hacky... 
        if (field is FieldImplementedByAll iba)
        {
            var types = Database.Query<EmailMessageEntity>().Where(a => a.Target != null).Select(a => a.Target!.Entity.GetType()).Distinct().ToHashSet();
            return types;
        }

        return new HashSet<Type>();
    }

    public static EmailMessageEntity WithAttachment(this EmailMessageEntity email, FilePathEmbedded filePath, string? contentId = null)
    {
        email.Attachments.Add(new EmailAttachmentEmbedded
        {
            ContentId = contentId ?? Guid.NewGuid().ToString(),
            File = filePath,
        });
        return email;
    }

    public static void SendMail(this IEmailModel model)
    {
        foreach (var email in model.CreateEmailMessage())
            GetEmailSender(email).Send(email);
    }

    public static void SendMail(this Lite<EmailTemplateEntity> template, ModifiableEntity entity)
    {
        foreach (var email in template.CreateEmailMessage(entity))
            GetEmailSender(email).Send(email);
    }

    public static void SendMail(this EmailMessageEntity email)
    {
        var template = email.Template?.Try(t => EmailTemplateLogic.EmailTemplatesLazy.Value.GetOrThrow(t));
        GetEmailSender(email).Send(email);
    }

    public static void SendMailAsync(this IEmailModel model)
    {
        foreach (var email in model.CreateEmailMessage())
            email.SendMailAsync();
    }

    public static void SendMailAsync(this Lite<EmailTemplateEntity> template, ModifiableEntity entity)
    {
        foreach (var email in template.CreateEmailMessage(entity))
            email.SendMailAsync();
    }

    public static void SendMailAsync(this EmailMessageEntity email)
    {
        using (OperationLogic.AllowSave<EmailMessageEntity>())
        {
            email.State = EmailMessageState.ReadyToSend;
            email.Save();
        }
    }

    public static SmtpClient SafeSmtpClient()
    {
        if (!EmailLogic.Configuration.SendEmails)
            throw new InvalidOperationException("EmailLogic.Configuration.SendEmails is set to false");

        //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
        return new SmtpClient()
        {
            ServicePoint = { MaxIdleTime = 2 }
        };
    }

    internal static SmtpClient SafeSmtpClient(string host, int port)
    {
        if (!EmailLogic.Configuration.SendEmails)
            throw new InvalidOperationException("EmailLogic.Configuration.SendEmails is set to false");

        //http://weblogs.asp.net/stanleygu/archive/2010/03/31/tip-14-solve-smtpclient-issues-of-delayed-email-and-high-cpu-usage.aspx
        return new SmtpClient(host, port)
        {
            ServicePoint = { MaxIdleTime = 2 }
        };
    }

    public static void SendAllAsync<T>(List<T> emails)
               where T : IEmailModel
    {
        var list = emails.SelectMany(a => a.CreateEmailMessage()).ToList();

        list.ForEach(a => a.State = EmailMessageState.ReadyToSend);

        using (OperationLogic.AllowSave<EmailMessageEntity>())
        {
            list.SaveList();
        }
    }

    class EmailGraph : Graph<EmailMessageEntity, EmailMessageState>
    {
        public static void Register()
        {
            GetState = m => m.State;

            new Construct(EmailMessageOperation.CreateMail)
            {
                ToStates = { EmailMessageState.Created },
                Construct = _ => new EmailMessageEntity
                {
                    State = EmailMessageState.Created,
                }
            }.Register();

            new ConstructFrom<EmailTemplateEntity>(EmailMessageOperation.CreateEmailFromTemplate)
            {
                ToStates = { EmailMessageState.Created },
                CanConstruct = et =>
                {
                    if (et.Model != null && EmailModelLogic.RequiresExtraParameters(et.Model))
                        return EmailMessageMessage._01requiresExtraParameters.NiceToString(typeof(EmailModelEntity).NiceName(), et.Model);

                    return null;
                },
                Construct = (et, args) =>
                {
                    var entity = args.TryGetArgC<ModifiableEntity>() ?? args.GetArg<Lite<Entity>>().RetrieveAndRemember();

                    var emailMessageEntity = et.ToLite().CreateEmailMessage(entity).FirstOrDefault();
                    if (emailMessageEntity == null)
                    {
                        throw new InvalidOperationException("No suitable recipients were found");
                    }
                    return emailMessageEntity;
                }
            }.Register();

            new Execute(EmailMessageOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                FromStates = { EmailMessageState.Created, EmailMessageState.Outdated },
                ToStates = { EmailMessageState.Draft },
                Execute = (m, _) =>
                {
                    m.State = EmailMessageState.Draft;
                }
            }.Register();

            new Execute(EmailMessageOperation.ReadyToSend)
            {
                CanBeNew = true,
                CanBeModified = true,
                FromStates = { EmailMessageState.Created, EmailMessageState.Draft, EmailMessageState.SentException, EmailMessageState.RecruitedForSending, EmailMessageState.Outdated },
                ToStates = { EmailMessageState.ReadyToSend },
                Execute = (m, _) =>
                {
                    m.SendRetries = 0;
                    m.Exception = null;
                    m.State = EmailMessageState.ReadyToSend;
                }
            }.Register();

            new Execute(EmailMessageOperation.Send)
            {
                CanExecute = m => m.State == EmailMessageState.Created || m.State == EmailMessageState.Draft ||
                     m.State == EmailMessageState.ReadyToSend || m.State == EmailMessageState.RecruitedForSending ||
                     m.State == EmailMessageState.Outdated ? null : EmailMessageMessage.TheEmailMessageCannotBeSentFromState0.NiceToString().FormatWith(m.State.NiceToString()),
                CanBeNew = true,
                CanBeModified = true,
                FromStates = { EmailMessageState.Created, EmailMessageState.Draft, EmailMessageState.ReadyToSend, EmailMessageState.Outdated },
                ToStates = { EmailMessageState.Sent },
                Execute = (m, _) => EmailLogic.GetEmailSender(m).Send(m)
            }.Register();


            new ConstructFrom<EmailMessageEntity>(EmailMessageOperation.ReSend)
            {
                ToStates = { EmailMessageState.Created },
                Construct = (m, _) =>
                {
                   
                    return new EmailMessageEntity
                    {
                        From = m.From.Clone(),
                        Recipients = m.Recipients.Select(r => r.Clone()).ToMList(),
                        Target = m.Target,
                        Subject = m.Subject,
                        Body = new BigStringEmbedded(m.Body.Text),
                        IsBodyHtml = m.IsBodyHtml,
                        Template = m.Template,
                        EditableMessage = m.EditableMessage,
                        State = EmailMessageState.Created,
                        Attachments = m.Attachments.Select(a => a.Clone()).ToMList()
                    };
                }
            }.Register();

            new Graph<EmailMessageEntity>.Delete(EmailMessageOperation.Delete)
            {
                Delete = (m, _) => m.Delete()
            }.Register();
        }
    }
}

public static class MimeMapping
{
    public static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        FileExtensionContentTypeProvider mimeConverter = new FileExtensionContentTypeProvider();

        return mimeConverter.Mappings.TryGetValue(extension ?? "", out var result) ? result : "application/octet-stream";
    }
}
