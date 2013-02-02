using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Net.Mail;
using Signum.Engine.Exceptions;

namespace Signum.Engine.Mailing
{
    public static class NewsletterLogic
    {
        static Expression<Func<IEmailOwnerDN, IQueryable<NewsletterDeliveryDN>>> NewsletterDeliveriesExpression =
            eo => Database.Query<NewsletterDeliveryDN>().Where(d => d.Recipient.RefersTo(eo));
        public static IQueryable<NewsletterDeliveryDN> NewsletterDeliveries(this IEmailOwnerDN eo)
        {
            return NewsletterDeliveriesExpression.Evaluate(eo);
        }

        static Expression<Func<NewsletterDN, IQueryable<NewsletterDeliveryDN>>> DeliveriesExpression =
            n => Database.Query<NewsletterDeliveryDN>().Where(nd => nd.Newsletter.RefersTo(n));
        public static IQueryable<NewsletterDeliveryDN> Deliveries(this NewsletterDN n)
        {
            return DeliveriesExpression.Evaluate(n);
        }

        static Expression<Func<NewsletterDN, IQueryable<NewsletterDeliveryDN>>> RemainingDeliveriesExpression =
            n =>n.Deliveries().Where(nd=>nd.Exception != null && !nd.Sent);
        public static IQueryable<NewsletterDeliveryDN> RemainingDeliveries(this NewsletterDN n)
        {
            return RemainingDeliveriesExpression.Evaluate(n);
        }

        static Expression<Func<NewsletterDN, IQueryable<NewsletterDeliveryDN>>> ExceptionDeliveriesExpression =
            n => Database.Query<NewsletterDeliveryDN>().Where(nd => nd.Newsletter.RefersTo(n));
        public static IQueryable<NewsletterDeliveryDN> ExceptionDeliveries(this NewsletterDN n)
        {
            return ExceptionDeliveriesExpression.Evaluate(n);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NewsletterDN>();
                sb.Include<NewsletterDeliveryDN>();
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(NewsletterOperation.Send, new NewsletterProcessAlgortihm());


                dqm[typeof(NewsletterDN)] = (from n in Database.Query<NewsletterDN>()
                                             select new
                                             {
                                                 Entity = n,
                                                 n.Id,
                                                 Nombre = n.Name,
                                                 Texto = n.HtmlBody.Etc(100),
                                                 Estado = n.State
                                             }).ToDynamic();


                dqm[typeof(NewsletterDeliveryDN)] = (from e in Database.Query<NewsletterDeliveryDN>()
                                                     select new
                                                     {
                                                         Entity = e,
                                                         e.Id,
                                                         e.Newsletter,
                                                         e.Recipient,
                                                         e.Sent,
                                                         e.SendDate,
                                                         e.Exception
                                                     }).ToDynamic();

                NewsletterGraph.Register();
                sb.AddUniqueIndex<NewsletterDeliveryDN>(nd => new { nd.Newsletter, nd.Recipient });

                Validator.PropertyValidator((NewsletterDN news) => news.HtmlBody).StaticPropertyValidation += (sender, pi) => ValidateTokens((NewsletterDN)sender, pi);
                Validator.PropertyValidator((NewsletterDN news) => news.Subject).StaticPropertyValidation += (sender, pi) => ValidateTokens((NewsletterDN)sender, pi);
            }
        }

        static string ValidateTokens(NewsletterDN newsletter, PropertyInfo pi)
        {
            if (pi.Is(() => newsletter.HtmlBody))
            {
                return AssertTokens(QueryLogic.ToQueryName(newsletter.Query.Key), newsletter.HtmlBody);
            }

            if (pi.Is(() => newsletter.Subject))
            {
                return AssertTokens(QueryLogic.ToQueryName(newsletter.Query.Key), newsletter.Subject);
            }

            return null;
        }

        static string AssertTokens(object queryName, string content)
        {
            List<string> tokens = FindTokens(content);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var str = tokens.Select(t =>
            {
                try
                {
                    QueryUtils.Parse(t, qd);
                    return null;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }).NotNull().ToString("\r\n");

            if (str.HasText())
                return str;

            return null;
        }

        public static List<QueryToken> GetTokens(object queryName, string content)
        {
            List<string> tokens = FindTokens(content);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            List<string> errors = new List<string>();
            return tokens.Select(t => QueryUtils.Parse(t, qd)).ToList();
        }

        public static readonly Regex TokenRegex = new Regex(@"\{(?<token>[^\}]*)\}");

        private static List<string> FindTokens(string content)
        {
            List<string> tokens = TokenRegex.Matches(content)
                .Cast<Match>().Select(m => m.Groups["token"].Value).ToList();
            return tokens;
        }
    }

    public class NewsletterGraph : Graph<NewsletterDN, NewsletterState>
    {
        public static void Register()
        {
            GetState = n => n.State;

            new ConstructFrom<NewsletterDN>(NewsletterOperation.Clone)
            {
                ToState = NewsletterState.Created,
                Construct = (n, _) => new NewsletterDN
                {
                    Name = n.Name,
                    From = n.From,
                    Query = n.Query,
                    HtmlBody = n.HtmlBody,
                    Subject = n.Subject,
                }
            }.Register();

            new Execute(NewsletterOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = new[] { NewsletterState.Created, NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, _) => n.State = NewsletterState.Saved
            }.Register();

            new Execute(NewsletterOperation.AddRecipients)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerDN>>>();
                    var existent = Database.Query<NewsletterDeliveryDN>().Where(d => d.Newsletter.RefersTo(n)).Select(d => d.Recipient).ToList();
                    p.Except(existent).Select(ie => new NewsletterDeliveryDN
                    {
                        Recipient = ie,
                        Newsletter = n.ToLite()
                    }).ToList().SaveList();

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Execute(NewsletterOperation.RemoveRecipients)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerDN>>>();
                    foreach (var eo in p.GroupsOf(20))
                    {
                        var col = Database.Query<NewsletterDeliveryDN>().Where(d =>
                            d.Newsletter.RefersTo(n) && eo.Any(i => i.Is(d.Recipient))).Select(d => d.ToLite()).ToList();
                        Database.DeleteList(col);
                    }

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Execute(NewsletterOperation.Send)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Sent,
                CanExecute = n => Database.Query<NewsletterDeliveryDN>().Any(d =>
                    d.Newsletter.RefersTo(n)) ? null : "There is not any delivery for this newsletter",
                Execute = (n, _) =>
                {
                    var process = ProcessLogic.Create(NewsletterOperation.Send, n);
                    process.Execute(ProcessOperation.Execute);

                    n.State = NewsletterState.Sent;
                }
            }.Register();
        }
    }

    class NewsletterProcessAlgortihm : IProcessAlgorithm
    {
        class SendLine
        {
            public ResultRow Row;
            public Lite<NewsletterDeliveryDN> Send;
            public string Email;
            public Exception Error;
        }

        public int NotificationSteps = 100;

        public void Execute(IExecutingProcess executingProcess)
        {
            NewsletterDN newsletter = (NewsletterDN)executingProcess.Data;

            var queryName = QueryLogic.ToQueryName(newsletter.Query.Key);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            var columns = new List<QueryToken>();
            columns.Add(QueryUtils.Parse("Entity.NewsletterDeliveries.Element", qd));
            columns.Add(QueryUtils.Parse("Entity.Email", qd));
            columns.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.Subject));
            columns.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.HtmlBody));

            columns = columns.Distinct().ToList();

            var resultTable = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                Filters = new List<Filter>
                { 
                    new Filter(qd, "Entity.NewsletterDeliveries.Element.Newsletter",  FilterOperation.EqualTo, newsletter.ToLite()),
                    new Filter(qd, "Entity.NewsletterDeliveries.Element.Sent", FilterOperation.EqualTo, false),
                },
                Orders = new List<Order>(),
                Columns = columns.Select(t => new Column(t, t.NiceName())).ToList(),
                ElementsPerPage = QueryRequest.AllElements,
            });

            var lines = resultTable.Rows.Select(r =>new SendLine
                {
                    Send = (Lite<NewsletterDeliveryDN>)r[0],
                    Email = (string)r[1],
                    Row = r,
                }).ToList();

            var dic = columns.Select((t, i) => KVP.Create(t.FullKey(), i)).ToDictionary();

            string overrideEmail = EmailLogic.OverrideEmailAddress();

            int processed = 0;
            foreach (var group in lines.GroupsOf(20))
            {
                processed += group.Count;

                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                if (overrideEmail != EmailLogic.DoNotSend)
                {
                    Parallel.ForEach(group, s =>
                    {
                        try
                        {
                            var client = newsletter.SMTPConfig.GenerateSmtpClient(true);
                            var message = new MailMessage();
                            message.From = new MailAddress(newsletter.From, newsletter.DisplayFrom);
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
                            s.Error = ex;
                        }
                    });
                }
                
                var failed = group.Extract(sl => sl.Error != null).GroupBy(sl => sl.Error, sl => sl.Send);
                foreach (var f in failed)
                {
                    var exLog = f.Key.LogException().ToLite();

                    Database.Query<NewsletterDeliveryDN>().Where(nd => f.Contains(nd.ToLite()))
                        .UnsafeUpdate(nd => new NewsletterDeliveryDN
                        {
                            Sent = true,
                            SendDate = TimeZoneManager.Now.TrimToSeconds(),
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
                            SendDate = TimeZoneManager.Now.TrimToSeconds(),
                        });
                }

                executingProcess.ProgressChanged(processed, lines.Count);
            }
        }
    }
}
