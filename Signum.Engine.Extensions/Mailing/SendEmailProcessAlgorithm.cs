using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Processes;
using Signum.Entities.Mailing;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Operations;

namespace Signum.Engine.Mailing
{
    public class SendEmailProcessAlgorithm : IProcessAlgorithm
    {
        public IProcessDataDN CreateData(object[] args)
        {
            List<Lite<EmailMessageDN>> messages = args.TryGetArgC<List<Lite<EmailMessageDN>>>(0);
         
            if (messages == null)
                throw new InvalidOperationException("No EmailMessageDN to process found");

            EmailPackageDN package = new EmailPackageDN()
            {
                NumLines = messages.Count,
                Name = args.TryGetArgC<string>(1),
                OverrideEmailAddress = EmailLogic.OnEmailAddress()
            }.Save();

            messages.Select(m => m.RetrieveAndForget()).Select(m => new EmailMessageDN()
            {
                Package = package.ToLite(),
                Recipient = m.Recipient,
                Body = m.Body,
                Subject = m.Subject,
                Template = m.Template,
                State = EmailState.Created
            }).SaveList();

            return package;
        }

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            EmailPackageDN package = (EmailPackageDN)executingProcess.Data;

            List<Lite<EmailMessageDN>> emails = (from email in Database.Query<EmailMessageDN>()
                                                 where email.Package == package.ToLite() && email.State == EmailState.Created
                                                 select email.ToLite()).ToList();


            using (EmailLogic.OverrideEmailAddressForProcess(package.OverrideEmailAddress))
            {
                int lastPercentage = 0;
                for (int i = 0; i < emails.Count; i++)
                {
                    if (executingProcess.Suspended)
                        return FinalState.Suspended;

                    EmailMessageDN ml = emails[i].RetrieveAndForget();

                    try
                    {
                        EmailLogic.SendMail(ml);
                    }
                    catch (Exception)
                    {
                        package.NumErrors++;
                        package.Save();
                    }

                    int percentage = (NotificationSteps * i) / emails.Count;
                    if (percentage != lastPercentage)
                    {
                        executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                        lastPercentage = percentage;
                    }
                }
            }

            return FinalState.Finished;
        }

        public int NotificationSteps = 100;
    }
}
