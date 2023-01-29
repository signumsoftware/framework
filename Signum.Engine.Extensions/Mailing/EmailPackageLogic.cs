using Signum.Entities.Mailing;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Authorization;
using Signum.Entities.Templating;
using Signum.Entities.Basics;

namespace Signum.Engine.Mailing;

public static class EmailPackageLogic
{
    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> Messages(this EmailPackageEntity p) => 
        As.Expression(() => Database.Query<EmailMessageEntity>().Where(a => a.Package.Is(p)));

    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> RemainingMessages(this EmailPackageEntity p) => 
        As.Expression(() => p.Messages().Where(a => a.State == EmailMessageState.RecruitedForSending || a.State == EmailMessageState.Draft || a.State == EmailMessageState.ReadyToSend));

    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> ExceptionMessages(this EmailPackageEntity p) => 
        As.Expression(() => p.Messages().Where(a => a.State == EmailMessageState.SentException));


    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Schema.Settings.AssertImplementedBy((ProcessEntity p) => p.Data, typeof(EmailPackageEntity));
            sb.Include<EmailPackageEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                });

            QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.Messages(), EmailMessageMessage.Messages);
            QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.RemainingMessages(), EmailMessageMessage.RemainingMessages);
            QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.ExceptionMessages(), EmailMessageMessage.ExceptionMessages);

            ProcessLogic.AssertStarted(sb);
            ProcessLogic.Register(EmailMessageProcess.CreateEmailsSendAsync, new CreateEmailsSendAsyncProcessAlgorithm());
            ProcessLogic.Register(EmailMessageProcess.SendEmails, new SendEmailProcessAlgorithm());

            new Graph<ProcessEntity>.ConstructFromMany<EmailMessageEntity>(EmailMessageOperation.ReSendEmails)
            {
                Construct = (messages, args) =>
                {
                    if (!messages.Any())
                        return null;

                    EmailPackageEntity emailPackage = new EmailPackageEntity()
                    {
                        Name = args.TryGetArgC<string>()
                    }.Save();

                    foreach (var m in messages.Select(m => m.Retrieve()))
                    {
                        new EmailMessageEntity()
                        {
                            Package = emailPackage.ToLite(),
                            From = m.From,
                            Recipients = m.Recipients.ToMList(),
                            Target = m.Target,
                            Body = new BigStringEmbedded(m.Body.Text),
                            IsBodyHtml = m.IsBodyHtml,
                            Subject = m.Subject,
                            Template = m.Template,
                            EditableMessage = m.EditableMessage,
                            State = EmailMessageState.RecruitedForSending,
                            Attachments = m.Attachments.Select(a => a.Clone()).ToMList()
                        }.Save();
                    }

                    return ProcessLogic.Create(EmailMessageProcess.SendEmails, emailPackage);
                }
            }.Register();
        }
    }

    public static ProcessEntity SendMultipleEmailsAsync(Lite<EmailTemplateEntity> template, List<Lite<Entity>> targets, ModelConverterSymbol? converter)
    {
        if (converter == null)
            return ProcessLogic.Create(EmailMessageProcess.CreateEmailsSendAsync, new PackageEntity().SetOperationArgs(new object[] { template }).CreateLines(targets));

        return ProcessLogic.Create(EmailMessageProcess.CreateEmailsSendAsync, new PackageEntity().SetOperationArgs(new object[] { template, converter }).CreateLines(targets));
    }
}


public class CreateEmailsSendAsyncProcessAlgorithm : IProcessAlgorithm
{
    public virtual void Execute(ExecutingProcess executingProcess)
    {
        PackageEntity package = (PackageEntity)executingProcess.Data!;

        var args = package.GetOperationArgs();
        var template = args.GetArg<Lite<EmailTemplateEntity>>();

        executingProcess.ForEachLine(package.Lines().Where(a => a.FinishTime == null), line =>
        {
            var emails = template.CreateEmailMessage(line.Target).ToList();
            foreach (var email in emails)
                email.SendMailAsync();

            line.Result = emails.Only()?.ToLite();
            line.FinishTime = Clock.Now;
            line.Save();
        });
    }
}

public class SendEmailProcessAlgorithm : IProcessAlgorithm
{
    public void Execute(ExecutingProcess executingProcess)
    {
        EmailPackageEntity package = (EmailPackageEntity)executingProcess.Data!;          

        List<Lite<EmailMessageEntity>> emails = package.RemainingMessages()
                                            .OrderBy(e => e.CreationDate)
                                            .Select(e => e.ToLite())
                                            .ToList();

        int counter = 0;
        using (AuthLogic.Disable())
        {

            foreach (var group in emails.Chunk(EmailLogic.Configuration.ChunkSizeSendingEmails))
            {
                var retrieved = group.RetrieveFromListOfLite();
                foreach (var m in retrieved)
                {
                    executingProcess.CancellationToken.ThrowIfCancellationRequested();
                    counter++;
                    try
                    {
                        using (var tr = Transaction.ForceNew())
                        {
                            EmailLogic.SendMail(m);
                            tr.Commit();
                        }
                        executingProcess.ProgressChanged(counter, emails.Count);
                    }
                    catch
                    {
                        try
                        {
                            if (m.SendRetries < EmailLogic.Configuration.MaxEmailSendRetries)
                            {
                                using (var tr = Transaction.ForceNew())
                                {
                                    var nm = m.ToLite().RetrieveAndRemember();
                                    nm.SendRetries += 1;
                                    nm.State = EmailMessageState.ReadyToSend;
                                    nm.Save();
                                    tr.Commit();
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }

}
