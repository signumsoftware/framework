using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Processes;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Mailing;
using Signum.Entities.Processes;
using System.Net.Mail;
using Signum.Utilities;
using System.Threading.Tasks;
using Signum.Entities.Mailing;

namespace Signum.Engine.Mailing
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

            var lines = (from e in Database.Query<NewsletterDeliveryDN>()
                         where e.Newsletter.RefersTo(newsletter) && !e.Sent
                         select new SendLine
                         {
                             Send = e.ToLite(),
                             Email = e.Recipient.Entity.Email,
                         }).ToList();

            int lastPercentage = 0;
            int numErrors = newsletter.NumErrors;
            int processed = 0;
            foreach (var groups in lines.GroupsOf(20))
            {
                processed += groups.Count;

                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                Parallel.ForEach(groups, s =>
                {
                    if (newsletter.OverrideEmail != EmailLogic.DoNotSend)
                    {
                        try
                        {
                            var client = newsletter.SMTPConfig.GenerateSmtpClient(true);
                            var message = new MailMessage();
                            message.From = new MailAddress(newsletter.From);
                            message.To.Add(newsletter.OverrideEmail ?? s.Email);
                            message.IsBodyHtml = true;
                            message.Body = newsletter.HtmlBody;
                            message.Subject = newsletter.Subject;
                            client.Send(message);
                        }
                        catch (Exception ex)
                        {
                            numErrors++;
                            s.Error = ex.Message;
                        }
                    }
                });

                using (var tr = new Transaction())
                {
                    int percentage = (NotificationSteps * processed) / lines.Count;
                    if (percentage != lastPercentage)
                    {
                        executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                        lastPercentage = percentage;
                    }

                    if (numErrors != 0)
                        newsletter.InDB().UnsafeUpdate(n => new NewsletterDN { NumErrors = numErrors });

                    var failed = groups.Extract(sl => sl.Error.HasText()).GroupBy(sl => sl.Error, sl => sl.Send);
                    foreach (var f in failed)
                    {
                        Database.Query<NewsletterDeliveryDN>().Where(nd => f.Contains(nd.ToLite()))
                            .UnsafeUpdate(nd => new NewsletterDeliveryDN
                            {
                                Sent = true,
                                SendDate = DateTime.Now.TrimToSeconds(),
                                Error = f.Key
                            });
                    }

                    if (groups.Any())
                    {
                        var sent = groups.Select(sl => sl.Send).ToList();
                        Database.Query<NewsletterDeliveryDN>().Where(nd => sent.Contains(nd.ToLite()))
                            .UnsafeUpdate(nd => new NewsletterDeliveryDN
                            {
                                Sent = true,
                                SendDate = DateTime.Now.TrimToSeconds(),
                            });
                    }

                    tr.Commit();
                }
            }

            return FinalState.Finished;
        }

        private class SendLine
        {
            public Lite<NewsletterDeliveryDN> Send;
            public string Email;
            public string Error;
        }
    }
}
