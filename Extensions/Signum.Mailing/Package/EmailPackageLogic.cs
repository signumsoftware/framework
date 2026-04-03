using Signum.Authorization;
using Signum.Mailing;
using Signum.Mailing.Templates;
using Signum.Processes;
using Signum.Templating;

namespace Signum.Mailing.Package;

public static class EmailPackageLogic
{
    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> EmailMessages(this EmailPackageEntity e) =>
    As.Expression(() => Database.Query<EmailMessageEntity>().Where(a => a.Mixin<EmailMessagePackageMixin>().Package.Is(e)));

    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> RemainingMessages(this EmailPackageEntity p) => 
        As.Expression(() => p.Messages().Where(a => a.State == EmailMessageState.RecruitedForSending || a.State == EmailMessageState.Draft || a.State == EmailMessageState.ReadyToSend));

    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> ExceptionMessages(this EmailPackageEntity p) => 
        As.Expression(() => p.Messages().Where(a => a.State == EmailMessageState.SentException));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.Settings.AssertImplementedBy((ProcessEntity p) => p.Data, typeof(EmailPackageEntity));
        MixinDeclarations.AssertDeclared(typeof(EmailMessageEntity), typeof(EmailMessagePackageMixin));

        sb.Include<EmailPackageEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
            });



        QueryLogic.Expressions.Register((EmailPackageEntity a) => a.EmailMessages(), () => typeof(EmailMessageEntity).NicePluralName());

        ExceptionLogic.DeleteLogs += ExceptionLogic_DeletePackages;

        QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.Messages(), EmailMessageMessage.Messages);
        QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.RemainingMessages(), EmailMessageMessage.RemainingMessages);
        QueryLogic.Expressions.Register((EmailPackageEntity ep) => ep.ExceptionMessages(), EmailMessageMessage.ExceptionMessages);

        ProcessLogic.AssertStarted(sb);
        ProcessLogic.Register(EmailMessageProcess.CreateEmailsSendAsync, new CreateEmailsSendAsyncProcessAlgorithm());
        ProcessLogic.Register(EmailMessageProcess.SendEmails, new SendEmailProcessAlgorithm());

        new Graph<ProcessEntity>.ConstructFromMany<EmailMessageEntity>(EmailMessagePackageOperation.ReSendEmails)
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
                    }
                    .SetMixin((EmailMessagePackageMixin m) => m.Package, emailPackage.ToLite())
                    .Save();
                }

                return ProcessLogic.Create(EmailMessageProcess.SendEmails, emailPackage);
            }
        }.Register();
    }

    public static ProcessEntity SendMultipleEmailsAsync(Lite<EmailTemplateEntity> template, List<Lite<Entity>> targets, ModelConverterSymbol? converter)
    {
        if (converter == null)
            return ProcessLogic.Create(EmailMessageProcess.CreateEmailsSendAsync, new PackageEntity().SetOperationArgs(new object[] { template }).CreateLines(targets));

        return ProcessLogic.Create(EmailMessageProcess.CreateEmailsSendAsync, new PackageEntity().SetOperationArgs(new object[] { template, converter }).CreateLines(targets));
    }

    public static void ExceptionLogic_DeletePackages(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        Database.Query<EmailPackageEntity>().Where(pack => !Database.Query<ProcessEntity>().Any(pr => pr.Data == pack) && !pack.EmailMessages().Any())
            .UnsafeDeleteChunksLog(parameters, sb, token);
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
                var retrieved = group.RetrieveList();
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
