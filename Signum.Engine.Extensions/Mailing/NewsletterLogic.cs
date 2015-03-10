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
using Signum.Entities.UserQueries;
using Signum.Engine.Templating;

namespace Signum.Engine.Mailing
{
    public static class NewsletterLogic
    {
        static Expression<Func<IEmailOwnerEntity, IQueryable<NewsletterDeliveryEntity>>> NewsletterDeliveriesExpression =
            eo => Database.Query<NewsletterDeliveryEntity>().Where(d => d.Recipient.RefersTo(eo));
        public static IQueryable<NewsletterDeliveryEntity> NewsletterDeliveries(this IEmailOwnerEntity eo)
        {
            return NewsletterDeliveriesExpression.Evaluate(eo);
        }

        static Expression<Func<NewsletterEntity, IQueryable<NewsletterDeliveryEntity>>> DeliveriesExpression =
            n => Database.Query<NewsletterDeliveryEntity>().Where(nd => nd.Newsletter.RefersTo(n));
        public static IQueryable<NewsletterDeliveryEntity> Deliveries(this NewsletterEntity n)
        {
            return DeliveriesExpression.Evaluate(n);
        }
        
        public static Func<NewsletterEntity, SmtpConfigurationEntity> GetStmpConfiguration;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Func<NewsletterEntity, SmtpConfigurationEntity> getSmtpConfiguration)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NewsletterEntity>();
                sb.Include<NewsletterDeliveryEntity>();

                NewsletterLogic.GetStmpConfiguration = getSmtpConfiguration;

                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(NewsletterProcess.SendNewsletter, new NewsletterProcessAlgorithm());

                dqm.RegisterQuery(typeof(NewsletterEntity), () =>
                 from n in Database.Query<NewsletterEntity>()
                 let p = n.LastProcess()
                 select new
                 {
                     Entity = n,
                     n.Id,
                     n.Name,
                     n.Subject,
                     Text = n.Text.Etc(100),
                     n.State,
                     NumDeliveries = n.Deliveries().Count(),
                     LastProcess = p,
                     NumErrors = n.Deliveries().Count(d => d.Exception(p) != null)
                 });           

                dqm.RegisterQuery(typeof(NewsletterDeliveryEntity), () =>
                    from e in Database.Query<NewsletterDeliveryEntity>()
                    let p = e.Newsletter.Entity.LastProcess()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Newsletter,
                        e.Recipient,
                        e.Sent,
                        e.SendDate,
                        LastProcess = p,
                        Exception = e.Exception(p)
                    });

                NewsletterGraph.Register();

                sb.AddUniqueIndex<NewsletterDeliveryEntity>(nd => new { nd.Newsletter, nd.Recipient });

                Validator.PropertyValidator((NewsletterEntity news) => news.Text).StaticPropertyValidation += (sender, pi) => ValidateTokens(sender, sender.Text);
                Validator.PropertyValidator((NewsletterEntity news) => news.Subject).StaticPropertyValidation += (sender, pi) => ValidateTokens(sender, sender.Subject);

                sb.Schema.EntityEvents<NewsletterEntity>().PreSaving += Newsletter_PreSaving;
            }
        }

        static string ValidateTokens(NewsletterEntity newsletter, string text)
        {
            var queryName = QueryLogic.ToQueryName(newsletter.Query.Key);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            try
            {
                string error;
                EmailTemplateParser.TryParse(text, qd, null, out error);

                return error.DefaultText(null);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


        static void Newsletter_PreSaving(NewsletterEntity newsletter, ref bool graphModified)
        {
            var queryname = QueryLogic.ToQueryName(newsletter.Query.Key);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryname);

            newsletter.Subject = EmailTemplateParser.Parse(newsletter.Subject, qd, null).ToString();
            newsletter.Text = EmailTemplateParser.Parse(newsletter.Text, qd, null).ToString();
        }
    }

    public class NewsletterGraph : Graph<NewsletterEntity, NewsletterState>
    {
        public static void Register()
        {
            GetState = n => n.State;

            new ConstructFrom<NewsletterEntity>(NewsletterOperation.Clone)
            {
                ToStates = { NewsletterState.Created },
                Construct = (n, _) => new NewsletterEntity
                {
                    Name = n.Name,
                    From = n.From,
                    DisplayFrom = n.DisplayFrom,
                    Query = n.Query,
                    Subject = n.Subject,
                    Text = n.Text,
                }
            }.Register();

            new Execute(NewsletterOperation.Save)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = { NewsletterState.Created, NewsletterState.Saved },
                ToStates = { NewsletterState.Saved },
                Execute = (n, _) => n.State = NewsletterState.Saved
            }.Register();

            new Execute(NewsletterOperation.AddRecipients)
            {
                FromStates = { NewsletterState.Saved },
                ToStates = { NewsletterState.Saved },
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerEntity>>>();
                    var existent = Database.Query<NewsletterDeliveryEntity>().Where(d => d.Newsletter.RefersTo(n)).Select(d => d.Recipient).ToList();
                    p.Except(existent).Select(ie => new NewsletterDeliveryEntity
                    {
                        Recipient = ie,
                        Newsletter = n.ToLite()
                    }).ToList().SaveList();

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Execute(NewsletterOperation.RemoveRecipients)
            {
                FromStates = { NewsletterState.Saved },
                ToStates = { NewsletterState.Saved },
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<NewsletterDeliveryEntity>>>();
                    foreach (var nd in p.GroupsOf(20))
                    {
                        Database.DeleteList(nd);
                    }

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Graph<ProcessEntity>.ConstructFrom<NewsletterEntity>(NewsletterOperation.Send)
            {
                CanConstruct = n =>
                {
                    if (n.Subject.IsNullOrEmpty())
                        return "Subject must be set";

                    if (n.Text.IsNullOrEmpty())
                        return "Text must be set";

                    if (!Database.Query<NewsletterDeliveryEntity>().Any(d => d.Newsletter.RefersTo(n)))
                        return "There is not any delivery for this newsletter";

                    return null;
                },
                Construct = (n, _) => ProcessLogic.Create(NewsletterProcess.SendNewsletter, n)
            }.Register();
        }
    }

    class NewsletterProcessAlgorithm : IProcessAlgorithm
    {
        class SendLine
        {
            public IGrouping<Lite<Entity>, ResultRow> Rows;
            public Lite<NewsletterDeliveryEntity> NewsletterDelivery;
            public EmailOwnerData Email;
            public Exception Exception;
        }

        public int NotificationSteps = 100;

        public void Execute(ExecutingProcess executingProcess)
        {
            NewsletterEntity newsletter = (NewsletterEntity)executingProcess.Data;

            var queryName = QueryLogic.ToQueryName(newsletter.Query.Key);

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            List<QueryToken> list = new List<QueryToken>();

            using (ExecutionMode.Global())
            {
                list.Add(QueryUtils.Parse("Entity", qd, 0));
                list.Add(QueryUtils.Parse(".".Combine("Entity", "NewsletterDeliveries", "Element"), qd, SubTokensOptions.CanElement));
                list.Add(QueryUtils.Parse(".".Combine("Entity", "EmailOwnerData"), qd, 0));

                EmailTemplateParser.Parse(newsletter.Subject, qd, null).FillQueryTokens(list);
                EmailTemplateParser.Parse(newsletter.Text, qd, null).FillQueryTokens(list);

                list = list.Distinct().ToList();
            }

            var columns = list.Select(qt => new Column(qt, null)).ToList();

            //var columns = new List<QueryToken>();
            //columns.Add(QueryUtils.Parse("Entity.NewsletterDeliveries.Element", qd, canAggregate: false));
            //columns.Add(QueryUtils.Parse("Entity.Email", qd, canAggregate: false));
            //columns.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.Subject));
            //columns.AddRange(NewsletterLogic.GetTokens(queryName, newsletter.Text));

            columns = columns.Distinct().ToList();

            var resultTable = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = queryName,
                Filters = new List<Filter>
                { 
                    new Filter(QueryUtils.Parse("Entity.NewsletterDeliveries.Element.Newsletter", qd, SubTokensOptions.CanElement),  FilterOperation.EqualTo, newsletter.ToLite()),
                    new Filter(QueryUtils.Parse("Entity.NewsletterDeliveries.Element.Sent", qd, SubTokensOptions.CanElement), FilterOperation.EqualTo, false),
                },
                Orders = new List<Order>(),
                Columns = columns,
                Pagination = new Pagination.All(),
            });

            var dicTokenColumn = resultTable.Columns.ToDictionary(rc => rc.Column.Token);

            var entityColumn = resultTable.Columns.SingleEx(c => c.Column.Token.FullKey() == "Entity");
            var deliveryColumn = resultTable.Columns.SingleEx(c => c.Column.Token.FullKey() == "Entity.NewsletterDeliveries.Element");
            var emailOwnerColumn = resultTable.Columns.SingleEx(c => c.Column.Token.FullKey() == "Entity.EmailOwnerData");
            
            var lines = resultTable.Rows.GroupBy(r => (Lite<Entity>)r[entityColumn]).Select(g => new SendLine
                {
                    NewsletterDelivery = (Lite<NewsletterDeliveryEntity>)g.DistinctSingle(deliveryColumn),
                    Email = (EmailOwnerData)g.DistinctSingle(emailOwnerColumn),
                    Rows = g,
                }).ToList();
            
            if (newsletter.SubjectParsedNode == null)
                newsletter.SubjectParsedNode = EmailTemplateParser.Parse(newsletter.Subject, qd, null);

            if (newsletter.TextParsedNode == null)
                newsletter.TextParsedNode = EmailTemplateParser.Parse(newsletter.Text, qd, null);

            var conf = EmailLogic.Configuration;

            int processed = 0;
            foreach (var group in lines.GroupsOf(20))
            {
                processed += group.Count;

                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                if (conf.SendEmails)
                {
                    Parallel.ForEach(group, s =>
                    {
                        try
                        {
                            var smtpConfig = NewsletterLogic.GetStmpConfiguration(newsletter);

                            var client = smtpConfig.GenerateSmtpClient();
                            var message = new MailMessage();
                            
                            if (newsletter.From.HasText())
                                message.From = new MailAddress(newsletter.From, newsletter.DisplayFrom);
                            else
                                message.From = smtpConfig.DefaultFrom.ToMailAddress();
                            
                            message.To.Add(conf.OverrideEmailAddress.DefaultText(s.Email.Email));

                            message.Subject = ((EmailTemplateParser.BlockNode)newsletter.SubjectParsedNode).Print(
                                new EmailTemplateParameters(null, null, dicTokenColumn, s.Rows)
                                {
                                    IsHtml = false,
                                });

                            message.Body = ((EmailTemplateParser.BlockNode)newsletter.TextParsedNode).Print(
                                new EmailTemplateParameters(null, null, dicTokenColumn, s.Rows)
                                {
                                    IsHtml = true,
                                });

                            message.IsBodyHtml = true;
                            
                            client.Send(message);
                        }
                        catch (Exception ex)
                        {
                            s.Exception = ex;
                        }
                    });
                }

                var failed = group.Extract(sl => sl.Exception != null);
                foreach (var f in failed)
                {
                    new ProcessExceptionLineEntity
                    {
                        Exception = f.Exception.LogException().ToLite(), 
                        Line = f.NewsletterDelivery,
                        Process = executingProcess.CurrentExecution.ToLite(),
                    }.Save();
                }


                var sent = group.Select(sl => sl.NewsletterDelivery).ToList();
                if (sent.Any())
                {
                    Database.Query<NewsletterDeliveryEntity>()
                        .Where(nd => sent.Contains(nd.ToLite()))
                        .UnsafeUpdate()
                        .Set(nd => nd.Sent, nd => true)
                        .Set(nd => nd.SendDate, nd => TimeZoneManager.Now.TrimToSeconds())
                        .Execute();
                }

                executingProcess.ProgressChanged(processed, lines.Count);
            }

            newsletter.State = NewsletterState.Sent;

            using (OperationLogic.AllowSave<NewsletterEntity>())
                newsletter.Save();
        }
    }
}
