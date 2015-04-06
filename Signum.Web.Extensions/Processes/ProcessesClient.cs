using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using Signum.Entities.Processes;
using Signum.Engine.Operations;
using Signum.Web.Omnibox;
using Signum.Web.PortableAreas;
using Signum.Utilities.Reflection;

namespace Signum.Web.Processes
{
    public static class ProcessClient
    {
        public static string ViewPrefix = "~/processes/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Processes/Scripts/Processes"); 

        public static void Start(bool packages, bool packageOperations)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(ProcessClient), "Processes");

                UrlsRepository.DefaultSFUrls.Add("processFromMany", url => url.Action((ProcessController pc)=>pc.ProcessFromMany()));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ProcessEntity>{ PartialViewName = e => ViewPrefix.FormatWith("Process"), },
                    new EntitySettings<ProcessAlgorithmSymbol>{ PartialViewName = e => ViewPrefix.FormatWith("ProcessAlgorithm") },
                });

                if (packages || packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageLineEntity> { PartialViewName = e => ViewPrefix.FormatWith("PackageLine") });
                }

                if (packages)
                {
                    Navigator.AddSetting(new EntitySettings<PackageEntity> { PartialViewName = e => ViewPrefix.FormatWith("Package") });
                }

                if (packageOperations)
                {
                    Navigator.AddSetting(new EntitySettings<PackageOperationEntity> { PartialViewName = e => ViewPrefix.FormatWith("PackageOperation") });

                    OperationClient.Manager.CustomizeMenuItem += CustomizeMenuItemForProcess;
                }
                
                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ProcessPanel", 
                    () => ProcessPermission.ViewProcessPanel.IsAuthorized(),
                    uh => uh.Action((ProcessController pc) => pc.View())));
            }
        }

        public static Dictionary<OperationSymbol, ContextualOperationSettingsBase> ProcessContextualOperartions;

        public static void Register<T>(this IEntityOperationSymbolContainer<T> entityOperation, ContextualOperationSettings<T> settings) where T : class, IEntity
        {
            ProcessContextualOperartions[entityOperation.Symbol] = settings;
        }

        static MenuItem CustomizeMenuItemForProcess(MenuItem mi)
        {
            if (!Navigator.IsViewable(typeof(PackageOperationEntity), null))
                return mi;

            var coc = (IContextualOperationContext)mi.Tag;

            if (coc.UntypedEntites.Count() <= 1)
                return mi;

            var settings = ProcessContextualOperartions.TryGetC(coc.OperationInfo.OperationSymbol);

            if (settings != null)
            {
                if (settings.HasIsVisible && !settings.OnIsVisible(coc))
                    return mi;

                if ((settings.HideOnCanExecute ?? coc.HideOnCanExecute) && coc.CanExecute.HasText())
                    return mi;
            }

            var clone = new MenuItemProcess(mi.Id)
            {
                Text = mi.Text,
                Html = mi.Html,
                Title = mi.Title,
                OnClick = mi.OnClick,
                Href = mi.Href,
                Order = mi.Order,
                Style = mi.Style,
                CssClass = mi.CssClass,
                Enabled = mi.Enabled,
                Tag = mi.Tag,
            };

            clone.HtmlProps.AddRange(mi.HtmlProps);

            clone.OnClickProcess = ProcessClient.Module["processFromMany"](coc.Options(), JsFunction.Event);

            return clone;
        }

        class MenuItemProcess : MenuItem
        {
            public JsFunction OnClickProcess { get; set; }

            public MenuItemProcess(string id) : base(id)
            {
            }

            protected override HtmlTag GetLinkElement()
            {
                var result = base.GetLinkElement();

                result.TagBuilder.InnerHtml += new HtmlTag("span").Id(this.Id + "_process").Class("glyphicon glyphicon-cog process-contextual-icon").Attr("aria-hidden", "true").ToHtml().ToHtmlString();

                return result;
            }

            public override MvcHtmlString ToHtml()
            {
                string eventHandler = "$('#" + Id + "_process" + "').on('mouseup', function(event){ event.stopPropagation(); if(event.which == 3) return; " + OnClickProcess.ToString() + " });";
                string dynamicCss = "SF.addCssDynamically('" + RouteHelper.New().Content("~/processes/Content/Processes.css?v=" + ScriptHtmlHelper.Manager.Version) + "')";

                var script = MvcHtmlString.Create("<script>" +
                eventHandler +
                dynamicCss + 
                "</script>");


                return base.ToHtml().Concat(script);
            }
        } 
    }
}