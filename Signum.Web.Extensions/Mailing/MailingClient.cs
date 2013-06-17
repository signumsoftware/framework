#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Engine.Mailing;
using System.Web.UI;
using System.IO;
using Signum.Entities.Mailing;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
#endregion

namespace Signum.Web.Mailing
{
    public static class MailingClient
    {
        public static string ViewPrefix = "~/Mailing/Views/{0}.cshtml";

        public static Mapping<EmailTemplateDN> EmailTemplateMessageTemplateMapping = ctx =>
        {
            var runtimeInfo = RuntimeInfo.FromFormValue(ctx.Parent.Parent.Parent.Parent.Inputs[EntityBaseKeys.RuntimeInfo]);
            return (EmailTemplateDN)runtimeInfo.ToLite().Retrieve();
        };

        private static QueryToken ParseQueryToken(string tokenString, string queryRuntimeInfoInput)
        {
            if (tokenString.IsNullOrEmpty())
                return null;

            var queryRuntimeInfo = RuntimeInfo.FromFormValue(queryRuntimeInfoInput);
            var queryName = QueryLogic.ToQueryName(((Lite<QueryDN>)queryRuntimeInfo.ToLite()).InDB(q => q.Key));
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            return QueryUtils.Parse(tokenString, qd, canAggregate: false);
        }

        static Mapping<QueryToken> EmailTemplateFromQueryTokenMapping = ctx =>
        {
            string tokenStr = "";
            foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).OrderBy())
                tokenStr += ctx.Parent.Inputs[key] + ".";
            while (tokenStr.EndsWith("."))
                tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

            if (tokenStr.IsNullOrEmpty())
                return null;

            return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
        };

        public static EntityMapping<QueryTokenDN> EmailTemplateFromQueryTokenDNMapping = new EntityMapping<QueryTokenDN>(false)
            .SetProperty(qt => qt.TryToken, MailingClient.EmailTemplateFromQueryTokenMapping);

        static Mapping<QueryToken> EmailTemplateRecipientsQueryTokenMapping = ctx =>
        {
            string tokenStr = "";
            foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).OrderBy())
                tokenStr += ctx.Parent.Inputs[key] + ".";
            while (tokenStr.EndsWith("."))
                tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

            if (tokenStr.IsNullOrEmpty())
                return null;

            return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
        };

        public static EntityMapping<QueryTokenDN> EmailTemplateRecipientsQueryTokenDNMapping = new EntityMapping<QueryTokenDN>(false)
            .SetProperty(qt => qt.TryToken, MailingClient.EmailTemplateRecipientsQueryTokenMapping);

        public static void Start(bool smtpConfig, bool newsletter)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(MailingClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EmailPackageDN>{ PartialViewName = e => ViewPrefix.Formato("EmailPackage")},
                    
                    new EntitySettings<EmailMessageDN>{ PartialViewName = e => ViewPrefix.Formato("EmailMessage")},
                    new EmbeddedEntitySettings<EmailAddressDN>{ PartialViewName = e => ViewPrefix.Formato("EmailAddress")},
                    new EmbeddedEntitySettings<EmailRecipientDN>{ PartialViewName = e => ViewPrefix.Formato("EmailRecipient")},

                    new EmbeddedEntitySettings<EmailTemplateConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("EmailLogicConfiguration")},
                });

                if (smtpConfig)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SmtpConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("SmtpConfiguration") },
                });

                if (newsletter)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    //new EntitySettings<NewsletterDN> { PartialViewName = e => ViewPrefix.Formato("Newsletter") },
                    new EntitySettings<NewsletterDeliveryDN> { PartialViewName = e => ViewPrefix.Formato("NewsletterDelivery") },
                });
            }
        }
    }
}
