using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
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
    }
}
