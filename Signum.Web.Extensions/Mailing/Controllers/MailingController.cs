using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Web.Operations;

namespace Signum.Web.Mailing
{
    public class MailingController : Controller
    {
        [HttpPost]
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix, int index)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var token = QueryUtils.Parse(tokenName, qd, canAggregate: false);

            var combo = SignumController.CreateHtmlHelper(this)
                .MailingInsertQueryTokenCombo(token, null, new Context(null, prefix), index + 1, qd, canAggregate: false);

            var content = combo.ToHtmlString();
            
            if (content.HasText())
                return Content(content);
            else
                return Content("<span>no-results</span>");
        }

        [HttpPost]
        public ActionResult RemoveRecipientsExecute(Lite<NewsletterDN> newsletter, string prefix)
        {
            var deliveries = this.ParseLiteKeys<NewsletterDeliveryDN>();

            newsletter.ExecuteLite(NewsletterOperation.RemoveRecipients, deliveries);

            return this.RedirectHttpOrAjax(Navigator.NavigateRoute(newsletter));
        }

        [HttpPost]
        public ActionResult CreateMailFromTemplateAndEntity(string prefix, string newPrefix)
        {
            var entity = Lite.Parse(Request["keys"]).Retrieve();

            var emailMessage = this.ExtractEntity<EmailTemplateDN>(prefix)
                .ConstructFrom<EmailMessageDN>(EmailMessageOperation.CreateMailFromTemplate, entity);

            return OperationClient.DefaultConstructResult(this, emailMessage, newPrefix);
        }
    }
}
