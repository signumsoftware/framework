using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Processes;
using Ski.Entities.Newsletter;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Mailing;
using Signum.Entities.Processes;
using System.Net.Mail;
using Signum.Utilities;
using System.Threading.Tasks;

namespace Ski.Logic.Newsletter
{
    class NewsletterProcessAlgortihm : IProcessAlgorithm
    {
        public IProcessDataDN CreateData(object[] args)
        {
            throw new NotImplementedException();
        }

        public int NotificationSteps = 100;

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            NewsletterDN newsletter = (NewsletterDN)executingProcess.Data;

            var lines = (from e in Database.Query<NewsLetterSendDN>()
                         where e.Newsletter.RefersTo(newsletter) && !e.Sent
                         select new SendLine
                         {
                             Send = e.ToLite(),
                             Email = e.EmailOwner.Entity.Email,
                         }).ToList();

            int lastPercentage = 0;
            foreach (var groups in lines.GroupsOf(20))
            {
                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                Parallel.ForEach(groups, s =>
                {
                    if (executingProcess.Suspended)
                        return;
                    try
                    {
                        var client = newsletter.SMTPConfig.GenerateSmtpClient(true);
                        client.Send(newsletter.From, s.Email, newsletter.Subject, newsletter.HtmlBody);
                        s.Send.InDB().UnsafeUpdate(sn => new NewsLetterSendDN
                        {
                            Sent = true,
                            SendDate = DateTime.Now.TrimToSeconds(),
                        });
                    }
                    catch (Exception ex)
                    {
                        newsletter.NumErrors++;
                        newsletter.Save();
                        s.Send.InDB().UnsafeUpdate(sn => new NewsLetterSendDN
                        {
                            Sent = true,
                            SendDate = DateTime.Now.TrimToSeconds(),
                            Error = ex.Message
                        });
                    }
                });

                int percentage = (NotificationSteps * 20) / lines.Count;
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                    lastPercentage = percentage;
                }
            }

            return FinalState.Finished;
        }

        private class SendLine
        {
            public Lite<NewsLetterSendDN> Send;
            public string Email;
        }
    }
}
