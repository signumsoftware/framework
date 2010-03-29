<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Operations" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <% Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.Menu); %>
    
    <% EntityHelp eh = (EntityHelp)Model; %>
    <form action="<%=HelpLogic.EntityUrl(eh.Type)%>/Save" id="form-save">
<div class="grid_16" id="entityName">
    <span class='shortcut'>[e:<%= eh.Type.Name%>]</span>
    <h1 title="<%= eh.Type.Namespace%>"><%=eh.Type.NiceName() %></h1> 
    <%= Html.TextArea("description", eh.Description, 5, 80, new { @class = "editable" })
                            + "<span class=\"editor\" id=\"description-editor\">" + eh.Description.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>") + "</span>"%>
</div>
<div class="clear"></div>
<div id="entityContent" class="grid_12">

    <% if (eh.Properties != null && eh.Properties.Count>0)
       { %>
        <div id="properties">
            <h2>Propiedades</h2>
            <dl>
            <%
                var a = TreeHelper.ToTreeS(eh.Properties, kvp =>
                    {
                        int index = kvp.Key.LastIndexOfAny(new []{'.', '/'});
                        if (index == -1)
                            return null;
                        string s = kvp.Key.Substring(0, index);
                        return new KeyValuePair<string,PropertyHelp>(s, eh.Properties[s]);
                    }); 
            ViewDataDictionary vdd = new ViewDataDictionary();
            vdd.Add("EntityName", eh.Type.Name);
            foreach (var node in a) Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.ViewEntityPropertyUrl, node, vdd);
             %>
            </dl>
        </div>
    <% }%>
    <% if (eh.Queries.TryCS(queries => queries.Count > 0) != null)
       { %>
        <div id="queries">
            <h2>Consultas</h2>
            <dl>
            <%
                foreach (var mq in eh.Queries)
                {%>
                <span class='shortcut'>[q:<%=QueryUtils.GetQueryName(mq.Key).ToString()%>]</span>
                <dt><%=QueryUtils.GetNiceQueryName(mq.Key)%></dt>
                <dd><img src='Images/Help/table.gif' title='Ver columnas' style='float:right' onclick="javascript:$(this).siblings('.query-columns').toggle('fast');" /><%=mq.Value.Info.WikiParse(HelpClient.DefaultWikiSettings)%>
                <%=Html.TextArea("q-" + QueryUtils.GetQueryName(mq.Key).ToString().Replace(".", "_"), mq.Value.UserDescription, new { @class = "editable" })%>
                <span class="editor" id="q-<%=QueryUtils.GetQueryName(mq.Key).ToString().Replace(".", "_")%>-editor">
                    <%=mq.Value.UserDescription.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>")%>
                </span>
                <div class='query-columns'><hr/>
                <table width="100%">
                <% foreach (var qc in mq.Value.Columns)
                   {%>
                    <tr><td><b><%=qc.Value.Name.NiceName()%></b> <%=qc.Value.Info%></td><td><%=qc.Value.Info %></td></tr>
                    <tr><td></td><td><%=Html.TextArea("c-" + QueryUtils.GetQueryName(mq.Key).ToString().Replace(".", "_") + "." + qc.Key, qc.Value.UserDescription, new { @class = "editable" })%>
                    <span class="editor" id="qc-<%=QueryUtils.GetQueryName(mq.Key).ToString().Replace(".", "_") + "." + qc.Key%>">
                        <%=qc.Value.UserDescription.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>")%>
                    </span>
                    </td></tr>
                   <%}%>
                   </table>
                   <hr/>
                </div>
                </dd>
                <%} %>
            </dl>
        </div>
    <% } %>
    <% if (eh.Operations != null && eh.Operations.Count > 0)
       { %>
        <div id="operations">
            <h2>
                Operaciones</h2>
            <dl>
                <%=eh.Operations.ToString(p =>
                   "<span class='shortcut'>[o:" + OperationDN.UniqueKey(p.Key) + "]</span>"
                   + "<dt>" + p.Key.NiceToString() + "</dt>"
                                       + "<dd>" + p.Value.Info.WikiParse(HelpClient.DefaultWikiSettings)
                   + Html.TextArea("o-" + OperationDN.UniqueKey(p.Key), p.Value.UserDescription, new { @class = "editable" })
                                                                               + "<span class=\"editor\" id=\"o-" + OperationDN.UniqueKey(p.Key).Replace(".", "_") + "-editor\">" + p.Value.UserDescription.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>") + "</span>"           
                   +"</dd>", "")%>
            </dl>
        </div>
    <% } %>

</div>
<div class="grid_4">
    <div class="sidebar">
        <h3>Temas relacionados</h3>
        <ul>
           <%
               List<Type> types = (List<Type>)ViewData["nameSpace"];
                   foreach (Type t in types)
                   {
                       if (t != eh.Type)
                       {  %>
                       <li><a href="<%=HelpLogic.EntityUrl(t) %>"><%=t.NiceName()%></a></li>
                     <% }
                       else
                       { %>
                       <li class="type-selected"><%=t.NiceName()%></li>
                       
                     <% } %>
                   <%}
            %>
        </ul>        
    </div>
    </div>
    <div class="clear"></div>
</form>
</asp:Content>
