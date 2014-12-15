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
using Signum.Entities.Mailing;

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
                    new EntitySettings<WordReportLogEntity>{ PartialViewName = e => ViewPrefix.FormatWith("WordReportLog")},
                });

                OperationClient.AddSetting(new EntityOperationSettings<Entity>(WordReportLogOperation.CreateWordReportFromEntity)
                {
                    IsVisible = ctx => HasTemplates(ctx.Entity.GetType()),
                    Click = ctx => CreateReportFromEntity(ctx.Options(), ctx.Entity.GetType(), ctx.Url, contextual: false),
                    Contextual =
                    {
                        IsVisible = ctx => HasTemplates(ctx.Entities.Single().EntityType),
                        Click = ctx => CreateReportFromEntity(ctx.Options(), ctx.Entities.Single().EntityType, ctx.Url, contextual: true),
                    }
                });

                OperationClient.AddSetting(new EntityOperationSettings<WordTemplateEntity>(WordReportLogOperation.CreateWordReportFromTemplate)
                {
                    Click = ctx => CreateReportFromTemplate(ctx.Options(), ctx.Entity.Query, ctx.Url, contextual: false),
                    Contextual =
                    {
                        Click = ctx => CreateReportFromTemplate(ctx.Options(), ctx.Entities.Single().InDB(a => a.Query), ctx.Url, contextual: true),
                    }
                });

                LinksClient.RegisterEntityLinks<Entity>((ident, ctx) => HasTemplates(ident.EntityType) ? new[] { new QuickLinkExplore(typeof(WordReportLogEntity), "Target", ident) } : null);
            }
        }

        private static JsFunction CreateReportFromTemplate(JsOperationOptions options, QueryEntity query, UrlHelper urlHelper, bool contextual)
        {
            return Module["createWordReportFromTemplate"](options, JsFunction.Event,
                new FindOptions(query.ToQueryName()).ToJS(options.prefix, "selectEntity"),
               urlHelper.Action((WordController c) => c.CreateWordReportFromTemplate()), contextual);
        }

        private static JsFunction CreateReportFromEntity(JsOperationOptions options, Type type, UrlHelper urlHelper, bool contextual)
        {
            return Module["createWordReportFromEntity"](options, JsFunction.Event,
                WordTemplateMessage.ChooseAReportTemplate.NiceToString(),
                WordTemplateLogic.TemplatesByType.Value.GetOrThrow(type.ToTypeEntity()).Select(a => a.ToChooserOption()).ToList(),
                urlHelper.Action((WordController c) => c.CreateWordReportFromEntity()), contextual);
        }

        private static bool HasTemplates(Type type)
        {
            return WordTemplateLogic.TemplatesByType.Value.ContainsKey(type.ToTypeEntity());
        }

        public static QueryTokenBuilderSettings GetQueryTokenBuilderSettings(QueryDescription qd, SubTokensOptions options)
        {
            return new QueryTokenBuilderSettings(qd, options)
            {
                ControllerUrl = RouteHelper.New().Action("NewSubTokensCombo", "Word"),
                Decorators = WordDecorators,
                RequestExtraJSonData = null,
            };
        }

        static void WordDecorators(QueryToken qt, HtmlTag option)
        {
            string canIf = CanIf(qt);
            if (canIf.HasText())
                option.Attr("data-if", canIf);

            string canForeach = CanForeach(qt);
            if (canForeach.HasText())
                option.Attr("data-foreach", canForeach);

            string canAny = CanAny(qt);
            if (canAny.HasText())
                option.Attr("data-any", canAny);
        }

        static string CanIf(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanForeach(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields.NiceToString();

            if (token.Key != "Element" || token.Parent == null || token.Parent.Type.ElementType() == null)
                return TemplateTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }

        static string CanAny(QueryToken token)
        {
            if (token == null)
                return TemplateTokenMessage.NoColumnSelected.NiceToString();

            if (token.HasAllOrAny())
                return TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny.NiceToString();

            return null;
        }
    }
}
