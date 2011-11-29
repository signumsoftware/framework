using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;

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

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NewsletterDN>();
                sb.Include<NewsletterDeliveryDN>();
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(NewsletterOperations.Send, new NewsletterProcessAlgortihm());


                dqm[typeof(NewsletterDN)] = (from n in Database.Query<NewsletterDN>()
                                             select new
                                             {
                                                 Entity = n.ToLite(),
                                                 n.Id,
                                                 Nombre = n.Name,
                                                 Texto = n.HtmlBody.Etc(100),
                                                 Estado = n.State
                                             }).ToDynamic();


                dqm[typeof(NewsletterDeliveryDN)] = (from e in Database.Query<NewsletterDeliveryDN>()
                                                     select new
                                                     {
                                                         Entity = e.ToLite(),
                                                         e.Id,
                                                         e.Newsletter,
                                                         e.Recipient,
                                                         e.Sent,
                                                         e.SendDate,
                                                         Error = e.Exception.Etc(50)
                                                     }).ToDynamic();

                NewsletterGraph.Register();
                sb.AddUniqueIndex<NewsletterDeliveryDN>(nd => new { nd.Newsletter, nd.Recipient });

                Validator.GetOrCreatePropertyPack((NewsletterDN news) => news.HtmlBody).StaticPropertyValidation += (sender, pi) => ValidateTokens((NewsletterDN)sender, pi);
                Validator.GetOrCreatePropertyPack((NewsletterDN news) => news.Subject).StaticPropertyValidation += (sender, pi) => ValidateTokens((NewsletterDN)sender, pi);
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

            new ConstructFrom<NewsletterDN>(NewsletterOperations.CreateFromThis)
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

            new Execute(NewsletterOperations.Save)
            {
                AllowsNew = true,
                Lite = false,
                FromStates = new[] { NewsletterState.Created, NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, _) => n.State = NewsletterState.Saved
            }.Register();

            new Execute(NewsletterOperations.AddRecipients)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerDN>>>(0);
                    var existent = Database.Query<NewsletterDeliveryDN>().Where(d => d.Newsletter.RefersTo(n)).Select(d => d.Recipient).ToList();
                    p.Except(existent).Select(ie => new NewsletterDeliveryDN
                    {
                        Recipient = ie,
                        Newsletter = n.ToLite()
                    }).ToList().SaveList();

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Execute(NewsletterOperations.RemoveRecipients)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, args) =>
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerDN>>>(0);
                    foreach (var eo in p.GroupsOf(20))
                    {
                        var col = Database.Query<NewsletterDeliveryDN>().Where(d =>
                            d.Newsletter.RefersTo(n) && eo.Any(i => i.Is(d.Recipient))).Select(d => d.ToLite()).ToList();
                        Database.DeleteList(col);
                    }

                    n.State = NewsletterState.Saved;
                }
            }.Register();

            new Execute(NewsletterOperations.Send)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Sent,
                CanExecute = n => Database.Query<NewsletterDeliveryDN>().Any(d =>
                    d.Newsletter.RefersTo(n)) ? null : "There is not any delivery for this newsletter",
                Execute = (n, _) =>
                {
                    var process = ProcessLogic.Create(NewsletterOperations.Send, n);
                    process.Execute(ProcessOperation.Execute);

                    n.OverrideEmail = EmailLogic.OnOverrideEmailAddress();
                    n.State = NewsletterState.Sent;
                }
            }.Register();
        }
    }
}
