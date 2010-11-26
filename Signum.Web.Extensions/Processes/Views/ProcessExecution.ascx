<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Processes" %>
<%: Html.DynamicCss("~/process/Content/Processes.css") %>
<%
    using (var e = Html.TypeContext<ProcessExecutionDN>())
    {
        Html.ValueLine(e, f => f.State, f => f.ReadOnly = true);
        Html.EntityLine(e, f => f.Process);
        Html.EntityLine(e, f => f.ProcessData, f => f.ReadOnly = true);
        Html.ValueLine(e, f => f.CreationDate);
        Html.ValueLine(e, f => f.PlannedDate, f => { f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.CancelationDate, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.QueuedDate, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.ExecutionStart, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.ExecutionEnd, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.SuspendDate, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.ExceptionDate, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.Exception, f => {f.HideIfNull = true; f.ReadOnly = true; });
        Html.ValueLine(e, f => f.Progress, f => f.ReadOnly = true);

        
        if (e.Value.State == ProcessState.Executing)
        {
%>
Progress:
<br />
<br />
<div class="progressContainer">
    <div class="progressBar" id="progressBar" style="height: 100%; width: <%=Math.Round((double)e.Value.Progress, 0)%>%;">
    </div>
</div>

<script type="text/javascript">
    $(function() {
        var idProcess = '<%=e.Value.Id%>';
        var idPrefix = '<%=e.ControlID %>';

        refreshUpdate(idProcess,idPrefix);
    })

    function refreshUpdate(idProcess, idPrefix) {
        setTimeout(function() {

            $.post("Process/getProgressExecution", { id: idProcess },
            function(data) {
                $("#progressBar").width(data + '%');
                if (data < 100) {
                    refreshUpdate(idProcess, idPrefix);
                }
                else {
                    if (empty(idPrefix)) {
                        ReloadEntity("Process/FinishProcessNormalPage", idPrefix);
                    }
                    else {
                        $("#" + idPrefix.compose("externalPopupDiv")).remove();
                        new ViewNavigator({
                            type: "ProcessExecutionDN",
                            id: idProcess,
                            prefix: idPrefix
                        }).createOk();
                    }
                }
            });
        }, 2000);
     }
</script>

<%
    }
    }
%>
