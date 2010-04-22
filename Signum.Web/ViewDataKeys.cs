using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web
{
    public static class ViewDataKeys
    {
        //public const string TypeContextKey = "sfTypeContextKey";
        public const string ResourcesRoute = "sfResourcesRoute";
        public const string SearchResourcesRoute = "sfSearchResourcesRoute";
        //public const string PopupPrefix = "sfPrefix";
        public const string WriteSFInfo = "sfWriteSFInfo";
        //public const string PopupSufix = "sfSufix";
        //public const string DivASustituir = "sfDivASustituir";
        //public const string LoadAll = "sfLoadAll";
        public const string GlobalErrors = "sfGlobalErrors"; //Key for Global Errors in ModelStateDictionary
        public const string GlobalValidationSummary = "sfGlobalValidationSummary";
        public const string PartialViewName = "sfPartialViewName";
       // public const string PopupInnerControlUrl = "sfPopupInnerControlUrl";
        //public const string StyleContext = "sfStyleContext";
        public const string PageTitle = "sfTitle";
        public const string PageDescription = "sfDescription";
        public const string CustomHtml = "sfCustomHtml";
        public const string OnOk = "sfOnOk";
        public const string OnCancel = "sfOnCancel";
        public const string BtnOk = "sfBtnOk";
        //public const string OnCancelS = "sfOnCancelS";
        //public const string BtnOkS = "sfBtnOkS";
        public const string BtnCancel = "sfBtnCancel";
        public const string NavigationButtons = "sfNavigationButtons";
        //public const string FilterColumns = "sfFilterColumns";
        public const string FindOptions = "sfFindOptions";
        public const string QueryDescription = "sfQueryDescription";
        public const string Top = "sfTop";
        //public const string QuerySettings = "sfQuerySettings";
        public const string Results = "sfResults";
        public const string EntityColumnIndex = "sfEntityColumnIndex";
        public const string EntityTypeName = "sfEntityTypeName";
        //public const string EntityTypeNiceName = "sfEntityTypeNiceName";
        //public const string EntityType = "sfEntityType";
        public const string AllowMultiple = "sfAllowMultiple";
        public const string Create = "sfCreate";
        public const string View = "sfView";
        public const string Formatters = "sfFormatters";
        public const string ChangeTicks = "sfChangeTicks";
        public const string Reactive = "sfReactive";
        public const string TabId = "sfTabId";

        public static string WindowPrefix(this HtmlHelper helper)
        {
            TypeContext tc = helper.ViewData.Model as TypeContext;
            if (tc == null)
                return null;
            else
                return tc.ControlID;
        }

        public static long? GetChangeTicks(this HtmlHelper helper, string controlID)
        {
            if (!helper.ViewData.ContainsKey(ViewDataKeys.ChangeTicks))
                return null;
            return ((Dictionary<string, long>)helper.ViewData[ViewDataKeys.ChangeTicks])
                .TryGetS(controlID);
        }

        /// <summary>
        /// Propagates LoadAll, Reactive, ChangeTicks
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="vdd"></param>
        public static void PropagateSFKeys(this HtmlHelper helper, ViewDataDictionary vdd)
        {
            if (helper.ViewData.ContainsKey(ViewDataKeys.Reactive))
                vdd[ViewDataKeys.Reactive] = true;
            
            if (helper.ViewData.ContainsKey(ViewDataKeys.ChangeTicks))
                vdd[ViewDataKeys.ChangeTicks] = helper.ViewData[ViewDataKeys.ChangeTicks];
        }
    }
}
