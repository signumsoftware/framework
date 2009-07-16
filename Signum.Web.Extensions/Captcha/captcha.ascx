<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Captcha" %>

<script type="text/javascript" language="javascript">
    function solicitarCaptcha() {
        $('#ajax-loader').show();
        $.ajax({
            type: "POST",
            url: "Captcha.aspx/Refresh",
            data: "",
            async: false,
            dataType: "html",
            success:
                   function(msg) {
                       $("#divCaptchaImage").html(msg);
                       $('#ajax-loader').hide();
                   },
            error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       $('#ajax-loader').hide();
                       alert("Error Occured! " + textStatus + " ; " + errorThrown + " ; " + XMLHttpRequest);
                   }
        });
    }
</script>

<div id="divCaptcha">
    <label class="labelLine">Escriba estos caracteres:</label>
    <div id="divCaptchaImage">
        <% Html.RenderPartial("captchaImage"); %>
    </div>
    <%= Html.Href("solicitarNuevoCaptcha", "Solicite un nuevo código", "javascript:solicitarCaptcha();", "Solicite un nuevo código", null, new Dictionary<string, object> { {"style", "float:left" } })%>
    <div class="clearall"></div>
    <label class="labelLine" for="captcha">Aquí:</label>
    <%= Html.TextBox("captcha", null, new {autocomplete="off"})%>
</div>