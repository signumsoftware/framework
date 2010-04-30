<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Ucalenda.Entities" %>
<%@ Import Namespace="Ucalenda.Web.Properties" %>

<% ResetPasswordRequestDN request = (ResetPasswordRequestDN)this.Model; %>
    <h2><%= HttpUtility.HtmlEncode(Resources.ConfirmationAscx_Confirmation) %></h2>

    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_Dear)%> <%= request.Name %>,<br />
    <%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_ThankYou)%></p>

    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_YourLoginNameIs)%><br /> 
    <strong><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_Username)%></strong><em><%= request.UniversityEmail %></em></p>

    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_ToConfirmAccount)%><br />
    <% string route = this.Context.Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/user/confirmMail") + "?user=" + request.UniversityEmail + "&code=" + request.Code;  %>
    <a href="<%=route%>"><%=route%></a></p>

    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_IfNothingHappens)%>p> 
    <p><strong><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_StillProblems)%></strong><br /> 
    <%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_IfConfirmationFails)%>
    </p>
    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmartionAscx_CopyRegistrationCode)%></p>
    <p><%= request.Code %></p>
    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_VisitPageAndPasteCode)%></p>

    <p><%=HttpUtility.HtmlEncode(Resources.ConfirmationAscx_BestRegards)%></p>
    <div class="email-footer">
        uCalenda
    </div>