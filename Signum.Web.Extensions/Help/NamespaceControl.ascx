<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Web.Help" %>
<ul>
    <li>
        <%
            NamespaceModel nm = (NamespaceModel)Model;
            if (nm.Types.Count > 0)
           { %>
            <h2><a href="Help/Namespace/<%= nm.Namespace %>"><%=nm.ShortNamespace%></a></h2>
        <% }
           else
           { %>
            <h2><%=nm.ShortNamespace%></h2>           
        <% } %> 
           <% if (nm.Types.Count > 0)
              { %>  
   
        <ul>
             <%
            foreach (Type type in nm.Types)
            {
                string urlName = HelpLogic.EntityUrl(type);
                string niceName = type.NiceName(); 
             %>
             <li>
                <a href="<%=urlName%>"><%=niceName%></a>
             </li>
            <%
            }
            %>
        </ul>
   
        <% } %>  
        <% if (nm.Namespaces.Count > 0) { %>
            <%
            foreach (NamespaceModel item in nm.Namespaces)
            {
                Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.NamespaceControlUrl, item); 
            }
         } %>            
    </li>
           
</ul>
