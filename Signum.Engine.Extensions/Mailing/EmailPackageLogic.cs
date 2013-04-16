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

namespace Signum.Engine.Mailing
{
    public static class EmailPackageLogic
    {
        static Expression<Func<EmailPackageDN, IQueryable<EmailMessageDN>>> MessagesExpression =
            p => Database.Query<EmailMessageDN>().Where(a => a.Package.RefersTo(p));
        public static IQueryable<EmailMessageDN> Messages(this EmailPackageDN p)
        {
            return MessagesExpression.Evaluate(p);
        }

        static Expression<Func<EmailPackageDN, IQueryable<EmailMessageDN>>> RemainingMessagesExpression =
            p => p.Messages().Where(a => a.State == EmailMessageState.Created);
        public static IQueryable<EmailMessageDN> RemainingMessages(this EmailPackageDN p)
        {
            return RemainingMessagesExpression.Evaluate(p);
        }

        static Expression<Func<EmailPackageDN, IQueryable<EmailMessageDN>>> ExceptionMessagesExpression =
            p => p.Messages().Where(a => a.State == EmailMessageState.Exception);
        public static IQueryable<EmailMessageDN> ExceptionMessages(this EmailPackageDN p)
        {
            return ExceptionMessagesExpression.Evaluate(p);
        }


        public static void StarProcesses(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailPackageDN>();

                dqm.RegisterExpression((EmailPackageDN ep) => ep.Messages());
                dqm.RegisterExpression((EmailPackageDN ep) => ep.RemainingMessages());
                dqm.RegisterExpression((EmailPackageDN ep) => ep.ExceptionMessages());

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(EmailMessageProcesses.SendEmails, new SendEmailProcessAlgorithm());

                new BasicConstructFromMany<EmailMessageDN, ProcessExecutionDN>(EmailMessageOperation.ReSendEmails)
                {
                    Construct = (messages, args) =>
                    {
                        EmailPackageDN emailPackage = new EmailPackageDN()
                        {
                            Name = args.TryGetArgC<string>()
                        }.Save();

                        foreach (var m in messages.Select(m => m.RetrieveAndForget()))
                        {
                            new EmailMessageDN()
                            {
                                Package = emailPackage.ToLite(),
                                Recipient = m.Recipient,
                                Body = m.Body,
                                Subject = m.Subject,
                                Template = m.Template,
                                State = EmailMessageState.Created
                            }.Save();
                        }

                        return ProcessLogic.Create(EmailMessageProcesses.SendEmails, emailPackage);
                    }
                }.Register();

                dqm.RegisterQuery(typeof(EmailPackageDN), () =>
                    from e in Database.Query<EmailPackageDN>()
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
            EmailPackageDN package = (EmailPackageDN)executingProcess.Data;


            

            List<Lite<EmailMessageDN>> emails = package.RemainingMessages()
                                                .Select(e => e.ToLite())
                                                .ToList();

            for (int i = 0; i < emails.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                EmailMessageDN ml = emails[i].RetrieveAndForget();

                EmailLogic.SendMail(ml);

                executingProcess.ProgressChanged(i, emails.Count);
            }
        }

        public int NotificationSteps = 100;
    }
}
