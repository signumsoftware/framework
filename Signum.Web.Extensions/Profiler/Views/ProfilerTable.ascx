<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Utilities.ExpressionTrees" %>
<%@ Import Namespace="Signum.Web.Profiler" %>
<%
    List<HeavyProfilerEntry> entries = (List<HeavyProfilerEntry>)Model;
    int[] indices = (int[])ViewData["indices"] ?? new int[0];

    var roles = entries.Select(a => a.GetDescendantRoles()).ToList();

    var allKeys = roles.SelectMany(a => a.Keys).Distinct().Order().ToList();
    
%>
<table class="tblResults">
    <thead>
        <tr>
            <th>
                Entry
            </th>
            <th>
                Type
            </th>
            <th>
                Method
            </th>
            <th>
                Role
            </th>
            <th>
                Time
            </th>
            <th>
                Childs
            </th>
            <%
                foreach (var k in allKeys)
                {
            %>
            <th>
                <%:k %>
                Childs
            </th>
            <%
                        
                }
            %>
            <th>
                Aditional Data
            </th>
        </tr>
    </thead>
    <tbody>
        <%            
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var rol = roles[i]; 
        %>
        <tr>
            <td>
                <%=Html.ProfilerEntry("View", indices.And(i))%>
            </td>
            <td>
                <%:entry.Type.TypeName()%>
            </td>
            <td>
                <%:entry.Method.Name%>
            </td>
             <td>
                <%:entry.Role%>
            </td>
            <td align="right">
                <%:entry.Elapsed.NiceToString()%>
            </td>
            <td align="right">
                <%:entry.GetEntriesResume().TryCC(r=>r.ToString(entry))%>
            </td>
             <%
                foreach (var k in allKeys)
                {
            %>
            <td align="right">
              <%:rol.TryGetC(k).TryCC(r => r.ToString(entry))%>
            </td>
            <%        
                }
            %>
            <td>
                <%:entry.AditionalData.TryCC(o => o.ToString().Left(50, false))%>
            </td>
        </tr>
    
    <% } %>
    </tbody>
</table>
<br />