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

        private static QueryTokenDN ParseQueryToken(string tokenString, string queryRuntimeInfoInput)
        {
            if (tokenString.IsNullOrEmpty())
                return null;

            var queryRuntimeInfo = RuntimeInfo.FromFormValue(queryRuntimeInfoInput);
            var queryName = QueryLogic.ToQueryName(((Lite<QueryDN>)queryRuntimeInfo.ToLite()).InDB(q => q.Key));
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

            return new QueryTokenDN(QueryUtils.Parse(tokenString, qd, canAggregate: false));
        }

        public static Mapping<QueryTokenDN> EmailTemplateFromQueryTokenMapping = ctx =>
        {
            string tokenStr = "";
            foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).OrderBy())
                tokenStr += ctx.Parent.Inputs[key] + ".";
            while (tokenStr.EndsWith("."))
                tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

            if (tokenStr.IsNullOrEmpty())
                return null;

            return ParseQueryToken(tokenStr, ctx.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", EntityBaseKeys.RuntimeInfo)]);
        };

        static EntityMapping<EmailTemplateContactDN> EmailTemplateContactMapping = new EntityMapping<EmailTemplateContactDN>(true)
            .SetProperty(ec => ec.Token, MailingClient.EmailTemplateFromQueryTokenMapping);

        public static Mapping<QueryTokenDN> EmailTemplateRecipientsQueryTokenMapping = ctx =>
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

        static EntityMapping<EmailTemplateRecipientDN> EmailTemplateRecipientMapping = new EntityMapping<EmailTemplateRecipientDN>(true)
            .SetProperty(ec => ec.Token, MailingClient.EmailTemplateRecipientsQueryTokenMapping);
        
        public static void Start(bool smtpConfig, bool newsletter, bool pop3Config)
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
                    new EntitySettings<EmailTemplateDN>() { PartialViewName = e => ViewPrefix.Formato("EmailTemplate") },
                    new EntitySettings<EmailMasterTemplateDN>{ PartialViewName =  e => ViewPrefix.Formato("EmailMasterTemplate") },
                    
                    new EmbeddedEntitySettings<EmailTemplateMessageDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateMessage"),
                        MappingDefault = new EntityMapping<EmailTemplateMessageDN>(true)
                            .SetProperty(etm => etm.Template, MailingClient.EmailTemplateMessageTemplateMapping)
                    },

                    new EmbeddedEntitySettings<EmailTemplateContactDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateContact"),
                        MappingDefault = EmailTemplateContactMapping,
                    },

                    new EmbeddedEntitySettings<EmailTemplateRecipientDN>() 
                    { 
                        PartialViewName = e => ViewPrefix.Formato("EmailTemplateRecipient"),
                        MappingDefault = EmailTemplateRecipientMapping
                    },

                    new EmbeddedEntitySettings<ClientCertificationFileDN> { PartialViewName = e => ViewPrefix.Formato("ClientCertificationFile")},
                });

                if (smtpConfig)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SmtpConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("SmtpConfiguration") },
                });

                if (newsletter)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<NewsletterDN> { PartialViewName = e => ViewPrefix.Formato("Newsletter") },
                    new EntitySettings<NewsletterDeliveryDN> { PartialViewName = e => ViewPrefix.Formato("NewsletterDelivery") },
                });

                if (pop3Config)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<Pop3ConfigurationDN> { PartialViewName = e => ViewPrefix.Formato("Pop3Configuration") },
                });
            }
        }

        public static MvcHtmlString MailingInsertQueryTokenBuilder(this HtmlHelper helper, QueryToken queryToken, Context context, QueryDescription qd, bool canAggregate = false)
        {
            var tokenPath = queryToken.FollowC(qt => qt.Parent).Reverse().NotNull().ToList();

            HtmlStringBuilder sb = new HtmlStringBuilder();

            for (int i = 0; i < tokenPath.Count; i++)
            {
                sb.AddLine(helper.MailingInsertQueryTokenCombo(i == 0 ? null : tokenPath[i - 1], tokenPath[i], context, i, qd, canAggregate));
            }

            sb.AddLine(helper.MailingInsertQueryTokenCombo(queryToken, null, context, tokenPath.Count, qd, canAggregate));

            return sb.ToHtml();
        }

        public static MvcHtmlString MailingInsertQueryTokenCombo(this HtmlHelper helper, QueryToken previous, QueryToken selected, Context context, int index, QueryDescription qd, bool canAggregate)
        {
            if (previous != null && SearchControlHelper.AllowSubTokens != null && !SearchControlHelper.AllowSubTokens(previous))
                return MvcHtmlString.Create("");

            var queryTokens = previous.SubTokens(qd, canAggregate);

            if (queryTokens.IsEmpty())
                return MvcHtmlString.Create("");

            var options = new HtmlStringBuilder();
            options.AddLine(new HtmlTag("option").Attr("value", "").SetInnerText("-").ToHtml());
            foreach (var qt in queryTokens)
            {
                var option = new HtmlTag("option")
                            .Attr("value", qt.Key)
                            .SetInnerText(qt.SubordinatedToString);

                if (selected != null && qt.Key == selected.Key)
                    option.Attr("selected", "selected");

                option.Attr("title", qt.NiceTypeName);
                option.Attr("style", "color:" + qt.TypeColor);

                string canIf = CanIf(qt);
                if (canIf.HasText())
                    option.Attr("data-if", canIf);

                string canForeach = CanForeach(qt);
                if (canForeach.HasText())
                    option.Attr("data-foreach", canForeach);

                string canWhere = CanWhere(qt);
                if (canWhere.HasText())
                    option.Attr("data-where", canWhere);

                options.AddLine(option.ToHtml());
            }

            string onChange = "SF.FindNavigator.newSubTokensCombo('{0}','{1}',{2},'{3}')".Formato(
                Navigator.ResolveWebQueryName(qd.QueryName), 
                context.ControlID, 
                index,
                RouteHelper.New().Action("NewSubTokensCombo", "Mailing"));
            
            HtmlTag dropdown = new HtmlTag("select")
                .IdName(context.Compose("ddlTokens_" + index))
                .InnerHtml(options.ToHtml())
                .Attr("onchange", onChange);

            if (selected != null)
            {
                dropdown.Attr("title", selected.NiceTypeName);
                dropdown.Attr("style", "color:" + selected.TypeColor);
            }

            return dropdown.ToHtml();
        }

        static string CanIf(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return EmailTemplateCanAddTokenMessage.YouCannotAddIfBlocksOnCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanForeach(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return EmailTemplateCanAddTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.NiceToString();

            if (token.Key != "Element" || token.Parent == null || token.Parent.Type.ElementType() == null)
                return EmailTemplateCanAddTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null; 
        }

        static string CanWhere(QueryToken token)
        {
            if (token == null)
                return EmailTemplateCanAddTokenMessage.NoColumnSelected.NiceToString();

            if (token.HasAllOrAny())
                return EmailTemplateCanAddTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }
    }
}
