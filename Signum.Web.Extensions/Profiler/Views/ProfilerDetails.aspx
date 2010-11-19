<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true"
    Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Utilities.ExpressionTrees" %>
<%@ Import Namespace="Signum.Web.Profiler" %>
<asp:Content ID="registerContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        Profiler Entry (<%
                            ProfilerEntryDetails details = (ProfilerEntryDetails)Model;


                            for (int i = 0; i < details.Indices.Length; i++)
                            {
        %><%= Html.ProfilerEntry(details.Indices[i].ToString(), details.Indices.Take(i + 1))%>
        <% 
}
        %>)</h2>
    <%=Html.ActionLink("(View all)", "ViewAll") %>
    <table class="tblResults">
        <tr>
            <th>
                Type
            </th>
            <td>
                <%=details.Entry.Type.TryCC(t=>t.TypeName())%>
            </td>
        </tr>
        <tr>
            <th>
                Method
            </th>
            <td>
                <%=details.Entry.Method.Name%>
            </td>
        </tr>
        <tr>
            <th>
                File Line
            </th>
            <td>
                <%=details.Entry.StackTrace.GetFrame(0).GetFileLineAndNumber()%>
            </td>
        </tr>
        <tr>
            <th>
                Role
            </th>
            <td>
                <%=details.Entry.Role%>
            </td>
        </tr>
        <tr>
            <th>
                Time
            </th>
            <td>
                <%=details.Entry.Stopwatch.Elapsed.NiceToString()%>
            </td>
        </tr>
         <tr>
            <th>
                Childs
            </th>
            <td>
                <%:details.Entry.GetEntriesResume().TryToString()%>
            </td>
        </tr>
        
        <%
            foreach (var kvp in details.Entry.GetDescendantRoles())
            {
               %> 
               <tr>
            <th>
                <%:kvp.Key %> Childs 
            </th>
            <td>
                <%:kvp.Value.ToString(details.Entry)%>
            </td>
        </tr>
               <% 
            }
            
            
             %>

    </table>
    <br />
    <% if (details.Entry.Entries != null)
       {%>
    <h3>
        Childs</h3>
    <%Html.RenderPartial(ProfileClient.ViewPath + "ProfilerTable", details.Entry.Entries, new ViewDataDictionary { { "indices", details.Indices } });%>
    <%} %>
    <h3>
        Aditional Data</h3>
    <div>
        <code>
            <pre>
        <%=details.Entry.AditionalData%>
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
                for (int i = 0; i < details.Entry.StackTrace.FrameCount; i++)
                {
                    var frame = details.Entry.StackTrace.GetFrame(i);
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
