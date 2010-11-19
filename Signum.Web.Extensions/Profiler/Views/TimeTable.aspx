<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<script type="text/javascript">
    $(function(){
        $(".tblResults").tablesorter();
    });
</script>
<a href="home/times2?clear=1">Limpiar resultados</a>
<table class="tblResults">
    <thead>
        <tr>
            <th>Name</th>
            <th>Entity</th>
            <th>Executions</th>
            <th>Last Time</th>
            <th>Min</th>
            <th>Average</th>
            <th>Max</th>
            <th>total</th>
        </tr>
    </thead>
    <%
        double[] percentiles = new double[]{0, 0.2, 0.8};
        if (TimeTracker.IdentifiedElapseds.Count > 0) {
            double max = TimeTracker.IdentifiedElapseds.OrderByDescending(p => p.Value.Average).First().Value.Average;
            foreach (KeyValuePair<string, TimeTrackerEntry> pair in TimeTracker.IdentifiedElapseds.OrderByDescending(p => p.Value.Average))
            {
                double percentile = pair.Value.Average / (double)max;
                int percentileIndex = 0;
                int i = 0;
                bool salir = false;
                while (i < percentiles.Length && !salir)
                {
                    if (percentile > percentiles[i]) percentileIndex = i;
                    if (percentile < percentiles[i]) salir = true;
                    i++;
                }
        %>
    <tbody>
        <tr class="percentile<%= percentileIndex %>">
            <td><span class="processName"><%= pair.Key.Split(' ')[0]%></span></td>
            <td><% if (pair.Key.Split(' ').Length > 1)
                   { %><span class="entityName"><%= pair.Key.Split(' ')[1]%></span><% } %></td>
            <td class="centered"><%= pair.Value.Count%></td>
            <td class="right"><%= pair.Value.LastTime%></td>
            <td class="right"><%= pair.Value.MinTime%></td>
            <td class="right"><%= pair.Value.Average%></td>
            <td class="right"><%= pair.Value.MaxTime%></td>
            <td class="right"><%= pair.Value.TotalTime%></td>            
        </tr>
    </tbody>
    <% 
        }
        } %>
</table>
</asp:Content>
