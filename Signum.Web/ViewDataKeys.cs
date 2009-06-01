using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class ViewDataKeys
    {
        public const string PopupPrefix = "sfPrefix";
        public const string GlobalErrors = "sfGlobalErrors"; //Key for Global Errors in ModelStateDictionary
        public const string MainControlUrl = "sfMainControlUrl";
       // public const string PopupInnerControlUrl = "sfPopupInnerControlUrl";
        public const string StyleContext = "sfStyleContext";
        public const string PageTitle = "sfTitle";
        public const string CustomHtml = "sfCustomHtml";
        public const string OnOk = "sfOnOk";
        public const string OnCancel = "sfOnCancel";
        public const string BtnOk = "sfBtnOk";
        public const string BtnCancel = "sfBtnCancel";
        
        public const string Columns = "sfColumns";
        public const string QueryName = "sfQueryName";
        public const string Filters = "sfFilters";
        public const string Results = "sfResults";

        public static string GlobalName(this HtmlHelper helper, string localName)
        {
            if (helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix))
                return helper.ViewData[ViewDataKeys.PopupPrefix].ToString() + localName;

            return localName;
        }

        public static bool IsContainedEntity(this HtmlHelper helper)
        {
            return helper.ViewData.ContainsKey(ViewDataKeys.PopupPrefix);
        }
    }

    
}
