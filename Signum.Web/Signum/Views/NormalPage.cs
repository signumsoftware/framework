﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
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
    //using WebMatrix.Data;
    //using WebMatrix.WebData;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/NormalPage.cshtml")]
    public class _Page_Signum_Views_NormalPage_cshtml : System.Web.Mvc.WebViewPage<TypeContext>
    {
#line hidden

        public _Page_Signum_Views_NormalPage_cshtml()
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


WriteLiteral("\r\n");


WriteLiteral("           \r\n");


DefineSection("head", () => {

WriteLiteral("\r\n    ");


Write(Html.ScriptsJs(
            "~/signum/Scripts/SF_Globals.js",
            "~/signum/Scripts/SF_Popup.js",
            "~/signum/Scripts/SF_Lines.js",
            "~/signum/Scripts/SF_ViewNavigator.js",
            "~/signum/Scripts/SF_FindNavigator.js",
            "~/signum/Scripts/SF_Validator.js",
            "~/signum/Scripts/SF_Operations.js"));

WriteLiteral("\r\n");


});

WriteLiteral("\r\n\r\n");


 using (Html.BeginForm())
{
    var ident = Model.UntypedValue as IdentifiableEntity;
    

WriteLiteral("    <div id=\"divNormalControl\" ");


                          Write(Html.Raw(ident != null? "data-isnew=\""+ident.IsNew.ToString().ToLower() +  "\"" : ""));

WriteLiteral(">\r\n");


       if(string.IsNullOrEmpty(ViewBag.Title))
        {
            ViewBag.Title = Model.UntypedValue.TryToString();
        }

WriteLiteral("     \r\n");


         if(string.IsNullOrEmpty(ViewBag.Title))
        {
            ViewBag.Title = Model.UntypedValue.TryToString();
        }


           Html.RenderPartial(Navigator.Manager.NormalControlView);

WriteLiteral("    </div>\r\n");


    

WriteLiteral("    <div class=\"clear\"></div>   \r\n");


}

        }
    }
}
