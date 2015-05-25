using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Mailing;
using Signum.Engine.Processes;
using Signum.Engine.Operations;
using Signum.Entities.Processes;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Engine.Scheduler;
using Signum.Engine.Authorization;

namespace Signum.Engine.Mailing
{
    public static class EmailPackageLogic
    {
        static Expression<Func<EmailPackageEntity, IQueryable<EmailMessageEntity>>> MessagesExpression =
            p => Database.Query<EmailMessageEntity>().Where(a => a.Package.RefersTo(p));
        public static IQueryable<EmailMessageEntity> Messages(this EmailPackageEntity p)
        {
            return MessagesExpression.Evaluate(p);
        }

        static Expression<Func<EmailPackageEntity, IQueryable<EmailMessageEntity>>> RemainingMessagesExpression =
            p => p.Messages().Where(a => a.State == EmailMessageState.RecruitedForSending);
        public static IQueryable<EmailMessageEntity> RemainingMessages(this EmailPackageEntity p)
        {
            return RemainingMessagesExpression.Evaluate(p);
        }

        static Expression<Func<EmailPackageEntity, IQueryable<EmailMessageEntity>>> ExceptionMessagesExpression =
            p => p.Messages().Where(a => a.State == EmailMessageState.SentException);
        public static IQueryable<EmailMessageEntity> ExceptionMessages(this EmailPackageEntity p)
        {
            return ExceptionMessagesExpression.Evaluate(p);
        }


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailPackageEntity>();

                dqm.RegisterExpression((EmailPackageEntity ep) => ep.Messages(), () => EmailMessageMessage.Messages.NiceToString());
                dqm.RegisterExpression((EmailPackageEntity ep) => ep.RemainingMessages(), () => EmailMessageMessage.RemainingMessages.NiceToString());
                dqm.RegisterExpression((EmailPackageEntity ep) => ep.ExceptionMessages(), () => EmailMessageMessage.ExceptionMessages.NiceToString());

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(EmailMessageProcess.SendEmails, new SendEmailProcessAlgorithm());

                new Graph<ProcessEntity>.ConstructFromMany<EmailMessageEntity>(EmailMessageOperation.ReSendEmails)
                {
                    Construct = (messages, args) =>
                    {
                        EmailPackageEntity emailPackage = new EmailPackageEntity()
                        {
                            Name = args.TryGetArgC<string>()
                        }.Save();

                        foreach (var m in messages.Select(m => m.RetrieveAndForget()))
                        {
                            new EmailMessageEntity()
                            {
                                Package = emailPackage.ToLite(),
                                From = m.From,
                                Recipients = m.Recipients.ToMList(),
                                Target = m.Target,
                                Body = m.Body,
                                IsBodyHtml = m.IsBodyHtml,
                                Subject = m.Subject,
                                Template = m.Template,
                                EditableMessage = m.EditableMessage,
                                State = EmailMessageState.RecruitedForSending
                            }.Save();
                        }

                        return ProcessLogic.Create(EmailMessageProcess.SendEmails, emailPackage);
                    }
                }.Register();

                dqm.RegisterQuery(typeof(EmailPackageEntity), () =>
                    from e in Database.Query<EmailPackageEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                    });
            }
        }

    }

    public class SendEmailProcessAlgorithm : IProcessAlgorithm
    {
        public void Execute(ExecutingProcess executingProcess)
        {
            EmailPackageEntity package = (EmailPackageEntity)executingProcess.Data;          

            List<Lite<EmailMessageEntity>> emails = package.RemainingMessages()
                                                .OrderBy(e => e.CreationDate)
                                                .Select(e => e.ToLite())
                                                .ToList();

            int counter = 0;
            using (AuthLogic.Disable())
            {

                foreach (var group in emails.GroupsOf(EmailLogic.Configuration.ChunkSizeSendingEmails))
                {
                    var retrieved = group.RetrieveFromListOfLite();
                    foreach (var m in retrieved)
                    {
                        executingProcess.CancellationToken.ThrowIfCancellationRequested();
                        counter++;
                        try
                        {
                            using (Transaction tr = Transaction.ForceNew())
                            {
                                EmailLogic.SenderManager.Send(m);
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
                                    using (Transaction tr = Transaction.ForceNew())
                                    {
                                        var nm = m.ToLite().Retrieve();
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
}
