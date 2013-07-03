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
        public PartialViewResult RemoveRecipients(string oldPrefix, string prefix)
        { 
            var newsletter = this.ExtractLite<NewsletterDN>(oldPrefix);

            ViewData[ViewDataKeys.OnOk] = JsFindNavigator.GetFor(prefix).hasSelectedItems(new JsFunction("items") 
            {
                Js.AjaxCall<MailingController>(mc => mc.RemoveRecipientsExecute(newsletter, prefix), 
                    "{ keys: items.map(function(i) { return i.key; }).join(',') }",
                    new JsFunction())
            }).ToJS();

            return Navigator.PartialFind(this, new FindOptions(typeof(NewsletterDeliveryDN), "Newsletter", newsletter), prefix);
        }

        [HttpPost]
        public JsonResult RemoveRecipientsExecute(Lite<NewsletterDN> newsletter, string prefix)
        {
            var deliveries = Navigator.ParseLiteKeys<NewsletterDeliveryDN>(Request["keys"]);

            newsletter.ExecuteLite(NewsletterOperation.RemoveRecipients, deliveries);

            return JsonAction.Redirect(Navigator.NavigateRoute(newsletter));
        }

        [HttpPost]
        public PartialViewResult CreateMailFromTemplate(string prefix, string oldPrefix)
        {
            var et = this.ExtractLite<EmailTemplateDN>(oldPrefix);

            var queryName = QueryLogic.ToQueryName(et.InDB(t => t.Query.Key));
           
            ViewData[ViewDataKeys.OnOk] = JsFindNavigator.GetFor(prefix).hasSelectedItems(new JsFunction("items") 
            {
                Js.Submit<MailingController>(mc => mc.CreateMailFromTemplateAndEntity(et, prefix), 
                    "{ keys: items.map(function(i) { return i.key; }).join(',') }")
            }).ToJS();

            return Navigator.PartialFind(this, new FindOptions(queryName), prefix);
        }

        [HttpPost]
        public ViewResult CreateMailFromTemplateAndEntity(Lite<EmailTemplateDN> emailTemplate, string prefix)
        {
            var entity = Lite.Parse(Request["keys"]).Retrieve();

            var emailMessage = emailTemplate.ConstructFromLite<EmailMessageDN>(EmailMessageOperation.CreateMailFromTemplate, entity);

            return Navigator.NormalPage(this, emailMessage);
        }
    }
}
