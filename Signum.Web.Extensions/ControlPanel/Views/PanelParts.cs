﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.261
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using Signum.Utilities;
    using Signum.Entities;
    using Signum.Web;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Configuration;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Caching;
    using System.Web.DynamicData;
    using System.Web.SessionState;
    using System.Web.Profile;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;
    using Signum.Web.Properties;
    using Signum.Entities.ControlPanel;
    using Signum.Web.ControlPanel;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/ControlPanel/Views/PanelParts.cshtml")]
    public class _Page_ControlPanel_Views_PanelParts_cshtml : System.Web.Mvc.WebViewPage<ControlPanelDN>
    {


        public _Page_ControlPanel_Views_PanelParts_cshtml()
        {
        }
        protected System.Web.HttpApplication ApplicationInstance
        {
            get
            {
                return ((System.Web.HttpApplication)(Context.ApplicationInstance));
            }
        }
        public override void Execute()
        {






 if (!Model.Parts.IsNullOrEmpty())
{
    int rowNumber = Model.Parts.Max(p => p.Row);
    int equalColumnWidth = (int)Math.Floor(((decimal)100 / Model.NumberOfColumns) - 1);

WriteLiteral("    <div id=\"sfCpContainer\">\r\n");


         for (int col = 1; col <= Model.NumberOfColumns; col++)
        {
            bool hasChart = Model.Parts.Any(p => p.Column == col && p.Content.GetType() == typeof(UserChartPartDN));

WriteLiteral("            <div class=\"sf-ftbl-column\"");


                                   Write(hasChart ? " style=min-width:" + equalColumnWidth + "%" : "");

WriteLiteral(" data-column=\"");


                                                                                                               Write(col);

WriteLiteral("\">\r\n");


                 for (int row = 1; row <= rowNumber; row++)
                {
                    PanelPart pp = Model.Parts.SingleOrDefaultEx(p => p.Row == row && p.Column == col);
                    if (pp != null)
                    {

WriteLiteral("                        <div class=\"sf-ftbl-part-container\">\r\n");


                               Html.RenderPartial(ControlPanelClient.ViewPrefix.Formato("PanelPart"), pp);    

WriteLiteral("                        </div>\r\n");


                    }
                }

WriteLiteral("            </div>\r\n");


        }

WriteLiteral("    </div>\r\n");



WriteLiteral("    <div class=\"clearall\"></div>\r\n");


}


        }
    }
}
