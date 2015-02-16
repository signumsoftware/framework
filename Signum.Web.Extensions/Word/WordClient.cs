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
using Signum.Engine.Word;
using System.Web.UI;
using System.IO;
using Signum.Entities.Word;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using Signum.Web.Operations;
using Signum.Web.UserQueries;
using System.Text.RegularExpressions;
using Signum.Entities.UserAssets;
using Signum.Web.UserAssets;
using Signum.Web.Basic;
using Signum.Entities.Processes;
using Signum.Web.Cultures;
using Signum.Entities.Templating;
using Signum.Web.Templating;

namespace Signum.Web.Word
{
    public static class WordClient
    {
        public static string ViewPrefix = "~/Word/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Word/Scripts/Word");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                CultureInfoClient.Start();

                Navigator.RegisterArea(typeof(WordClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<WordTemplateEntity>{ PartialViewName = e => ViewPrefix.FormatWith("WordTemplate")},              
                    new EntitySettings<SystemWordTemplateEntity>{ },
                    new EntitySettings<WordTransformerSymbol>{ },
                    new EntitySettings<WordConverterSymbol>{ },
                });
                OperationClient.AddSetting(new EntityOperationSettings<WordTemplateEntity>(WordTemplateOperation.CreateWordReport)
                {
                    Group = EntityOperationGroup.None,
                    Click = ctx => Module["createWordReportFromTemplate"](ctx.Options(), JsFunction.Event,
                        new FindOptions(ctx.Entity.Query.ToQueryName()).ToJS(ctx.Prefix, "New"),
                        ctx.Url.Action((WordController mc) => mc.CreateWordReport()))
                });
            }
        }

        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd, SubTokensOptions options)
        {
            return new QueryTokenBuilderSettings(qd, options)
            {
                ControllerUrl = RouteHelper.New().Action("NewSubTokensCombo", "Word"),
                Decorators = TemplatingClient.TemplatingDecorators,
                RequestExtraJSonData = null,
            };
        }
    }
}
