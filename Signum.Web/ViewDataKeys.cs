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
        public const string GlobalErrors = "sfGlobalErrors"; //Key for Global Errors in ModelStateDictionary
        public const string Title = "Title";

        public const string ViewMode = "sfViewMode";
        public const string FindMode = "sfFindMode";
        public const string AvoidReturnView = "sfAvoidReturnView";
        public const string AvoidReturnRedirect = "sfAvoidReturnRedirect";

        public const string ManualToolbarButtons = "sfManualToolbarButtons";
        public const string FindOptions = "sfFindOptions";
        public const string FilterOptions = "sfFilterOptions";
        public const string QueryTokenSettings = "sfQueryTokenSettings";
        public const string Navigate = "sfNavigate";
        public const string AllowSelection = "sfAllowMultiple";
        public const string Pagination = "sfPagination";
        public const string QueryDescription = "sfQueryDescription";
        public const string QueryName = "sfQueryName";
        public const string Results = "sfResults";
        public const string QueryRequest = "sfQueryRequest";
        public const string MultipliedMessage = "sfMultipliedMessage";
        public const string Formatters = "sfFormatters";
        public const string EntityFormatter = "sfEntityFormatter";
        public const string RowAttributes = "sfRowAttributes";
        public const string TabId = "sfTabId";
        public const string PartialViewName = "sfPartialViewName";
        public const string EntityState = "sfEntityState";
        public const string WriteEntityState = "sfWriteEntityState";
        public const string InPopup = "sfIsPopup";
        public const string AvoidFullScreenButton = "sfAvoidFullScreenButton";
        public const string ShowOperations = "sfShowOperations";
        public const string RequiresSaveOperation = "sfRequiresSaveOperation";
        public const string FiltersVisible = "sfFiltersVisible";
        public const string ShowAddColumn = "sfShowAddColumn";
        public static string ShowFooter = "sfShowFooter";

        public static string WindowPrefix(this HtmlHelper helper)
        {
            TypeContext tc = helper.ViewData.Model as TypeContext;
            if (tc == null)
                return null;
            else
                return tc.Prefix;
        }

      
    }


}
