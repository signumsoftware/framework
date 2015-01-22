using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Word;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Word;
using Signum.Utilities;
using Signum.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Web.Operations;

namespace Signum.Web.Word
{
    public class WordController : Controller
    {
        [HttpPost]
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix, int options)
        {
            object queryName = Finder.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var token = QueryUtils.Parse(tokenName, qd, (SubTokensOptions)options);

            var combo = FinderController.CreateHtmlHelper(this)
                .QueryTokenBuilderOptions(token, new Context(null, prefix), WordClient.GetQueryTokenBuilderSettings(qd, (SubTokensOptions)options));

            return Content(combo.ToHtmlString());
        }

        [HttpPost]
        public ActionResult CreateWordReportFromEntity()
        {
            Lite<WordTemplateEntity> templateLite = Lite.Parse<WordTemplateEntity>(Request["keys"]);

            WordTemplateEntity template = templateLite.GetFromCache();

            byte[] bytes = template.CreateReport(this.ExtractEntity<Entity>());

            return File(bytes, template.FileNameFormat);
        }

        [HttpPost]
        public ActionResult CreateWordReportFromTemplate()
        {
            var entity = Lite.Parse<Entity>(Request["keys"]).Retrieve();

            var template = this.ExtractEntity<WordTemplateEntity>();

            byte[] bytes = template.CreateReport(entity);

            return File(bytes, template.FileNameFormat);
        }
    }
}
