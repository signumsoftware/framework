﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18051
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.Extensions.Help.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 2 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Engine;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Engine.Help;
    
    #line default
    #line hidden
    using Signum.Entities;
    
    #line 7 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Entities.Basics;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Entities.DynamicQuery;
    
    #line default
    #line hidden
    
    #line 1 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Entities.Reflection;
    
    #line default
    #line hidden
    using Signum.Utilities;
    using Signum.Web;
    
    #line 5 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Web.Extensions;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Help\Views\ViewEntity.cshtml"
    using Signum.Web.Help;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "1.5.4.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Help/Views/ViewEntity.cshtml")]
    public partial class ViewEntity : System.Web.Mvc.WebViewPage<dynamic>
    {
        public ViewEntity()
        {
        }
        public override void Execute()
        {








DefineSection("head", () => {

WriteLiteral("\r\n    ");


            
            #line 10 "..\..\Help\Views\ViewEntity.cshtml"
Write(Html.ScriptCss("~/help/Content/help.css"));

            
            #line default
            #line hidden
WriteLiteral("\r\n    ");


            
            #line 11 "..\..\Help\Views\ViewEntity.cshtml"
Write(Html.ScriptsJs("~/signum/Scripts/SF_Globals.js"));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


});

WriteLiteral("\r\n");


            
            #line 13 "..\..\Help\Views\ViewEntity.cshtml"
   Html.RenderPartial(HelpClient.Menu);

            
            #line default
            #line hidden

            
            #line 14 "..\..\Help\Views\ViewEntity.cshtml"
   EntityHelp eh = (EntityHelp)Model;
   ViewBag.Title = eh.Type.NiceName();


            
            #line default
            #line hidden
WriteLiteral("<form action=\"");


            
            #line 17 "..\..\Help\Views\ViewEntity.cshtml"
         Write(HelpLogic.EntityUrl(eh.Type));

            
            #line default
            #line hidden
WriteLiteral("/Save\" id=\"form-save\">\r\n<div id=\"entityName\">\r\n    <span class=\'shortcut\'>[e:");


            
            #line 19 "..\..\Help\Views\ViewEntity.cshtml"
                         Write(eh.Type.Name);

            
            #line default
            #line hidden
WriteLiteral("]</span>\r\n    <h1 title=\"");


            
            #line 20 "..\..\Help\Views\ViewEntity.cshtml"
          Write(eh.Type.Namespace);

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 20 "..\..\Help\Views\ViewEntity.cshtml"
                              Write(eh.Type.NiceName());

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n    ");


            
            #line 21 "..\..\Help\Views\ViewEntity.cshtml"
Write(Html.TextArea("description", eh.Description, 5, 80, new { @class = "editable" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n    <span class=\"editor\" id=\"description-editor\">\r\n        ");


            
            #line 23 "..\..\Help\Views\ViewEntity.cshtml"
   Write(Html.WikiParse(eh.Description, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n    </span>\r\n</div>\r\n<div id=\"entityContent\" class=\"help_left\">\r\n");


            
            #line 27 "..\..\Help\Views\ViewEntity.cshtml"
     if (eh.Properties != null && eh.Properties.Count > 0)
    {

            
            #line default
            #line hidden
WriteLiteral("        <div id=\"properties\">\r\n            <h2>\r\n                Propiedades</h2>" +
"\r\n            <dl>\r\n");


            
            #line 33 "..\..\Help\Views\ViewEntity.cshtml"
                   
        var a = TreeHelper.ToTreeS(eh.Properties, kvp =>
        {
            string s = kvp.Key.TryBeforeLast('.') ?? kvp.Key.TryBeforeLast('/');
            if(s == null)
                return null;

            if (s.StartsWith("[")) // Mixin
                return null;
            
            return new KeyValuePair<string, PropertyHelp>(s, eh.Properties[s]);
        });
        ViewDataDictionary vdd = new ViewDataDictionary();
        vdd.Add("EntityName", eh.Type.Name);
                

            
            #line default
            #line hidden

            
            #line 48 "..\..\Help\Views\ViewEntity.cshtml"
                 foreach (var node in a)
                {
                    Html.RenderPartial(HelpClient.ViewEntityPropertyUrl, node, vdd);
                }

            
            #line default
            #line hidden
WriteLiteral("            </dl>\r\n        </div>\r\n");


            
            #line 54 "..\..\Help\Views\ViewEntity.cshtml"
    }

            
            #line default
            #line hidden

            
            #line 55 "..\..\Help\Views\ViewEntity.cshtml"
     if (eh.Queries.TryCS(queries => queries.Count > 0) != null)
    {

            
            #line default
            #line hidden
WriteLiteral("        <div id=\"queries\">\r\n            <h2>\r\n                Consultas</h2>\r\n   " +
"         <dl>\r\n");


            
            #line 61 "..\..\Help\Views\ViewEntity.cshtml"
                 foreach (var mq in eh.Queries)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <span class=\'shortcut\'>[q:");


            
            #line 63 "..\..\Help\Views\ViewEntity.cshtml"
                                         Write(QueryUtils.GetQueryUniqueKey(mq.Key));

            
            #line default
            #line hidden
WriteLiteral("]</span>\r\n");



WriteLiteral("                    <dt>");


            
            #line 64 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(QueryUtils.GetNiceName(mq.Key));

            
            #line default
            #line hidden
WriteLiteral("</dt>\r\n");



WriteLiteral("                    <dd>\r\n                        <img src=\'help/Images/table.gif" +
"\' title=\'Ver columnas\' style=\'float: right\' onclick=\"javascript:$(this).siblings" +
"(\'.query-columns\').toggle(\'fast\');\" />\r\n                        ");


            
            #line 67 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(Html.WikiParse(mq.Value.Info, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        ");


            
            #line 68 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(Html.TextArea("q-" + QueryUtils.GetQueryUniqueKey(mq.Key).Replace(".", "_"), mq.Value.UserDescription, new { @class = "editable" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        <span class=\"editor\" id=\"q-");


            
            #line 69 "..\..\Help\Views\ViewEntity.cshtml"
                                              Write(QueryUtils.GetQueryUniqueKey(mq.Key).Replace(".", "_"));

            
            #line default
            #line hidden
WriteLiteral("-editor\">\r\n                            ");


            
            #line 70 "..\..\Help\Views\ViewEntity.cshtml"
                       Write(Html.WikiParse(mq.Value.UserDescription, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </span>\r\n                        <div class=\'query-colu" +
"mns\'>\r\n                            <hr />\r\n                            <table wi" +
"dth=\"100%\">\r\n");


            
            #line 75 "..\..\Help\Views\ViewEntity.cshtml"
                                 foreach (var qc in mq.Value.Columns)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <tr>\r\n                                       " +
" <td>\r\n                                            <b>");


            
            #line 79 "..\..\Help\Views\ViewEntity.cshtml"
                                          Write(qc.Value.Name.NiceName());

            
            #line default
            #line hidden
WriteLiteral("</b>\r\n                                        </td>\r\n                            " +
"            <td> ");


            
            #line 81 "..\..\Help\Views\ViewEntity.cshtml"
                                        Write(Html.WikiParse(qc.Value.Info, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </td>\r\n                                " +
"    </tr>\r\n");



WriteLiteral("                                    <tr>\r\n                                       " +
" <td>\r\n                                        </td>\r\n                          " +
"              <td>");


            
            #line 87 "..\..\Help\Views\ViewEntity.cshtml"
                                       Write(Html.TextArea("c-" + QueryUtils.GetQueryUniqueKey(mq.Key).Replace(".", "_") + "." + qc.Key, qc.Value.UserDescription, new { @class = "editable" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                            <span class=\"editor\" id=\"qc-");


            
            #line 88 "..\..\Help\Views\ViewEntity.cshtml"
                                                                    Write(QueryUtils.GetQueryUniqueKey(mq.Key).Replace(".", "_") + "." + qc.Key);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                ");


            
            #line 89 "..\..\Help\Views\ViewEntity.cshtml"
                                           Write(Html.WikiParse(qc.Value.UserDescription, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                            </span>\r\n                          " +
"              </td>\r\n                                    </tr>\r\n");


            
            #line 93 "..\..\Help\Views\ViewEntity.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                            </table>\r\n                            <hr />\r\n       " +
"                 </div>\r\n                    </dd>\r\n");


            
            #line 98 "..\..\Help\Views\ViewEntity.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </dl>\r\n        </div>\r\n");


            
            #line 101 "..\..\Help\Views\ViewEntity.cshtml"
    }

            
            #line default
            #line hidden

            
            #line 102 "..\..\Help\Views\ViewEntity.cshtml"
     if (eh.Operations != null && eh.Operations.Count > 0)
    {

            
            #line default
            #line hidden
WriteLiteral("        <div id=\"operations\">\r\n            <h2>\r\n                Operaciones</h2>" +
"\r\n            <dl>\r\n");


            
            #line 108 "..\..\Help\Views\ViewEntity.cshtml"
                 foreach (var p in eh.Operations)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <span class=\'shortcut\'>[o:");


            
            #line 110 "..\..\Help\Views\ViewEntity.cshtml"
                                         Write(OperationDN.UniqueKey(p.Key));

            
            #line default
            #line hidden
WriteLiteral("]</span>\r\n");



WriteLiteral("                    <dt>");


            
            #line 111 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(p.Key.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("</dt>\r\n");



WriteLiteral("                    <dd>\r\n                        ");


            
            #line 113 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(Html.WikiParse(p.Value.Info, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        ");


            
            #line 114 "..\..\Help\Views\ViewEntity.cshtml"
                   Write(Html.TextArea("o-" + OperationDN.UniqueKey(p.Key), p.Value.UserDescription, new { @class = "editable" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        <span class=\"editor\" id=\"o-");


            
            #line 115 "..\..\Help\Views\ViewEntity.cshtml"
                                              Write(OperationDN.UniqueKey(p.Key).Replace(".", "_"));

            
            #line default
            #line hidden
WriteLiteral("-editor\">\r\n                            ");


            
            #line 116 "..\..\Help\Views\ViewEntity.cshtml"
                       Write(Html.WikiParse(p.Value.UserDescription, HelpClient.DefaultWikiSettings));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </span>\r\n                    </dd>\r\n");


            
            #line 119 "..\..\Help\Views\ViewEntity.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </dl>\r\n        </div>\r\n");


            
            #line 122 "..\..\Help\Views\ViewEntity.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("</div>\r\n<div class=\"help_right\">\r\n    <div class=\"sidebar\">\r\n        <h3>\r\n      " +
"      Temas relacionados</h3>\r\n        <ul>\r\n");


            
            #line 129 "..\..\Help\Views\ViewEntity.cshtml"
               List<Type> types = (List<Type>)ViewData["nameSpace"];

            
            #line default
            #line hidden

            
            #line 130 "..\..\Help\Views\ViewEntity.cshtml"
             foreach (Type t in types)
            {
                if (t != eh.Type)
                {

            
            #line default
            #line hidden
WriteLiteral("                <li><a href=\"");


            
            #line 134 "..\..\Help\Views\ViewEntity.cshtml"
                        Write(HelpLogic.EntityUrl(t));

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 134 "..\..\Help\Views\ViewEntity.cshtml"
                                                 Write(t.NiceName());

            
            #line default
            #line hidden
WriteLiteral("</a></li>\r\n");


            
            #line 135 "..\..\Help\Views\ViewEntity.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                <li class=\"type-selected\">");


            
            #line 138 "..\..\Help\Views\ViewEntity.cshtml"
                                     Write(t.NiceName());

            
            #line default
            #line hidden
WriteLiteral("</li>\r\n");


            
            #line 139 "..\..\Help\Views\ViewEntity.cshtml"
                }
            }

            
            #line default
            #line hidden
WriteLiteral("        </ul>\r\n    </div>\r\n</div>\r\n<div class=\"clearall\">\r\n</div>\r\n</form>\r\n");


        }
    }
}
#pragma warning restore 1591
