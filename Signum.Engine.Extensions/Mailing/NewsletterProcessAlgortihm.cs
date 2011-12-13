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
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Entities.Logging;
using Signum.Engine.Logging;

namespace Signum.Engine.Mailing
{
    class NewsletterProcessAlgortihm : IProcessAlgorithm
    {
        class SendLine
        {
            public ResultRow Row; 
            public Lite<NewsletterDeliveryDN> Send;
            public string Email;
            public Exception Error;
        }

        public IProcessDataDN CreateData(object[] args)
        {
            throw new NotImplementedException();
        }

        public int NotificationSteps = 100;

        public FinalState Execute(IExecutingProcess executingProcess)
        {
            NewsletterDN newsletter = (NewsletterDN)executingProcess.Data;

            var queryName = QueryLogic.ToQueryName(newsletter.Query.Key);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var newsletterDeliveryElementToken = QueryUtils.Parse("Entity.NewsletterDeliveries.Element", qd);
            var newsletterToken = QueryUtils.Parse("Entity.NewsletterDeliveries.Element.Newsletter", qd);
            var emailToken = QueryUtils.Parse("Entity.Email", qd); 

            var tokens = new List<QueryToken>();
            tokens.Add(newsletterDeliveryElementToken);
            tokens.Add(emailToken); 
            tokens.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.Subject)); 
            tokens.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.HtmlBody));

            tokens = tokens.Distinct().ToList();

            var resultTable = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                Filters = new List<Filter>
                { 
                    new Filter
                    { 
                        Token = newsletterToken,
                        Operation = FilterOperation.EqualTo, 
                        Value = newsletter.ToLite(), 
                    }
                },
                Orders = new List<Order>(),
                Columns = tokens.Select(t => new Column(t, t.NiceName())).ToList(),
                ElementsPerPage = null,
            }); 

            var lines = resultTable.Rows.Select(r => 
                new SendLine 
                {
                    Send = (Lite<NewsletterDeliveryDN>)r[0],  
                    Email = (string)r[1],  
                    Row = r,
                });

            var dic = tokens.Select((t,i)=>KVP.Create(t.FullKey(), i)).ToDictionary();

            string overrideEmail = EmailLogic.OverrideEmailAddress();

            int lastPercentage = 0;
            int numErrors = newsletter.NumErrors;
            int processed = 0;
            foreach (var group in lines.GroupsOf(20))
            {
                processed += group.Count;

                if (executingProcess.Suspended)
                    return FinalState.Suspended;

                if (overrideEmail != EmailLogic.DoNotSend)
                {
                    Parallel.ForEach(group, s =>
                    {
                        try
                        {
                            var client = newsletter.SMTPConfig.GenerateSmtpClient(true);
                            var message = new MailMessage();
                            message.From = new MailAddress(newsletter.From, newsletter.DiplayFrom);
                            message.To.Add(overrideEmail ?? s.Email);
                            message.IsBodyHtml = true;
                            message.Body = NewsletterLogic.TokenRegex.Replace(newsletter.HtmlBody, m =>
                            {
                                var index = dic[m.Groups["token"].Value];
                                return s.Row[index].TryToString();
                            });
                            message.Subject = NewsletterLogic.TokenRegex.Replace(newsletter.Subject, m =>
                            {
                                var index = dic[m.Groups["token"].Value];
                                return s.Row[index].TryToString();
                            });
                            client.Send(message);
                        }
                        catch (Exception ex)
                        {
                            numErrors++;
                            s.Error = ex;
                        }
                    });
                }


                int percentage = (NotificationSteps * processed) / lines.Count();
                if (percentage != lastPercentage)
                {
                    executingProcess.ProgressChanged(percentage * 100 / NotificationSteps);
                    lastPercentage = percentage;
                }

                if (numErrors != 0)
                    newsletter.InDB().UnsafeUpdate(n => new NewsletterDN { NumErrors = numErrors });

                var failed = group.Extract(sl => sl.Error != null).GroupBy(sl => sl.Error, sl => sl.Send);
                foreach (var f in failed)
                {
                    var exLog = f.Key.LogException().ToLite();

                    Database.Query<NewsletterDeliveryDN>().Where(nd => f.Contains(nd.ToLite()))
                        .UnsafeUpdate(nd => new NewsletterDeliveryDN
                        {
                            Sent = true,
                            SendDate = DateTime.Now.TrimToSeconds(),
                            Exception = exLog
                        });
                }

                if (group.Any())
                {
                    var sent = group.Select(sl => sl.Send).ToList();
                    Database.Query<NewsletterDeliveryDN>().Where(nd => sent.Contains(nd.ToLite()))
                        .UnsafeUpdate(nd => new NewsletterDeliveryDN
                        {
                            Sent = true,
                            SendDate = DateTime.Now.TrimToSeconds(),
                        });
                }
            }

            return FinalState.Finished;
        }
    }
}
