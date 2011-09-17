using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using System.Reflection;
using Ski.Entities.Newsletter;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Entities.Mailing;

namespace Ski.Logic.Newsletter
{
    public static class NewsletterLogic
    {
        public static void Start(DynamicQueryManager dqm, SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NewsletterDN>();
                sb.Include<NewsLetterSendDN>();
                ProcessLogic.AssertStarted(sb);
                ProcessLogic.Register(NewsletterOperations.Send, new NewsletterProcessAlgortihm());


                dqm[typeof(NewsletterDN)] = (from n in Database.Query<NewsletterDN>()
                                             select new 
                                             {
                                                Entity = n.ToLite(),
                                                n.Id,
                                                Nombre = n.Name,
                                                Texto = n.HtmlBody.Etc(50),
                                                Estado = n.State
                                             }).ToDynamic();


                dqm[typeof(NewsLetterSendDN)] = (from e in Database.Query<NewsLetterSendDN>()
                                             select new
                                             {
                                                 Entity = e.ToLite(),
                                                 e.Id,
                                                 e.Newsletter,
                                                 e.EmailOwner,
                                                 e.Sent,
                                                 e.SendDate
                                             }).ToDynamic();
            }
        }
    }

    public class NewsletterGraph : Graph<NewsletterDN, NewsletterState>
    {
        public static void Register()
        {
            GetState = n => n.State;

            new Execute(NewsletterOperations.Save) 
            {
                AllowsNew = true,
                Lite = false,
                FromStates = new [] { NewsletterState.Saved },
                ToState = NewsletterState.Saved,
                Execute = (n, _) => n.State = NewsletterState.Saved
            }.Register();

            new Execute(NewsletterOperations.Send)
            {
                FromStates = new[] { NewsletterState.Saved },
                ToState = NewsletterState.Sent,
                Execute = (n, args) => 
                {
                    var p = args.GetArg<List<Lite<IEmailOwnerDN>>>(0);
                    p.Select(ie => new NewsLetterSendDN 
                    {
                        EmailOwner = ie,
                        Newsletter = n.ToLite()
                    }).ToList().SaveList();

                    var process = ProcessLogic.Create(NewsletterProcessOperations.Send, n);

                    process.ToLite().ExecuteLite(ProcessOperation.Execute);

                    n.State = NewsletterState.Sent;
                }
            }.Register();
        }
    }
}
