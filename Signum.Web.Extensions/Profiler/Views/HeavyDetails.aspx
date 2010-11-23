<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true"
    Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Utilities.ExpressionTrees" %>
<%@ Import Namespace="Signum.Web.Profiler" %>
<asp:Content ID="registerContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        Profiler Entry (<%
        HeavyProfilerEntry entry = (HeavyProfilerEntry)Model;

       foreach (var e in entry.FollowC(a=>a.Parent).Skip(1).Reverse())
	    {
           %><%=Html.ProfilerEntry(e.Index.ToString(), e.FullIndex())%>.<%    
	    }
        %>
        <%=entry.Index.ToString()%>)</h2>
    <%=Html.ActionLink("(View all)", "ViewAll") %>
    <table class="tblResults">
        <tr>
            <th>
                Type
            </th>
            <td>
                <%=entry.Type.TryCC(t => t.TypeName())%>
            </td>
        </tr>
        <tr>
            <th>
                Method
            </th>
            <td>
                <%=entry.Method.Name%>
            </td>
        </tr>
        <tr>
            <th>
                File Line
            </th>
            <td>
                <%=entry.StackTrace.GetFrame(0).GetFileLineAndNumber()%>
            </td>
        </tr>
        <tr>
            <th>
                Role
            </th>
            <td>
                <%=entry.Role%>
            </td>
        </tr>
        <tr>
            <th>
                Time
            </th>
            <td>
                <%=entry.Elapsed.NiceToString()%>
            </td>
        </tr>
         <tr>
            <th>
                Childs
            </th>
            <td>
                <%:entry.GetEntriesResume().TryToString()%>
            </td>
        </tr>
        
        <%
            foreach (var kvp in entry.GetDescendantRoles())
            {
               %> 
               <tr>
            <th>
                <%:kvp.Key %> Childs 
            </th>
            <td>
                <%:kvp.Value.ToString(entry)%>
            </td>
        </tr>
               <% 
            }
            
            
             %>

    </table>
    <br />
    <% if (entry.Entries != null)
       {%>
    <h3>
        Childs</h3>
    <%Html.RenderPartial(ProfileClient.ViewPath + "ProfilerTable", entry.Entries);%>
    <%} %>
    <h3>
        Aditional Data</h3>
    <div>
        <code>
            <pre>
        <%=entry.AditionalData%>
    </pre>
        </code>
    </div>
    <br />
    <h3>
        StackTrace</h3>
    <table class="tblResults">
        <thead>
            <tr>
                <th>
                    Type
                </th>
                <th>
                    Method
                </th>
                <th>
                    FileLine
                </th>
            </tr>
        </thead>
        <tbody>
            <%            
                for (int i = 0; i < entry.StackTrace.FrameCount; i++)
                {
                    var frame = entry.StackTrace.GetFrame(i);
            %>
            <tr>
                <td>
                    <%=frame.GetMethod().DeclaringType.TryCC(t=>t.TypeName()) %>
                </td>
                <td>
                    <%=frame.GetMethod().Name%>
                </td>
                <td>
                    <%=frame.GetFileLineAndNumber()%>
                </td>
            </tr>
            <% } %>
        </tbody>
    </table>
    <br />
</asp:Content>
