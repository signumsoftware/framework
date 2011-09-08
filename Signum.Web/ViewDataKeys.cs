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
        public const string WriteSFInfo = "sfWriteSFInfo";
        public const string GlobalErrors = "sfGlobalErrors"; //Key for Global Errors in ModelStateDictionary
        public const string Title = "Title";
        public const string CustomHtml = "sfCustomHtml";
        public const string OnOk = "sfOnOk";
        public const string FindOptions = "sfFindOptions";
        public const string QueryDescription = "sfQueryDescription";
        public const string QueryName = "sfQueryName";
        public const string Results = "sfResults";
        public const string Formatters = "sfFormatters";
        public const string ChangeTicks = "sfChangeTicks";
        public const string Reactive = "sfReactive";
        public const string TabId = "sfTabId";
        public const string PartialViewName = "sfPartialViewName";
        
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
