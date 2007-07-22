Imports Microsoft.VisualBasic

Imports Framework.Usuarios.DN

Public Class ControladorSesionLN

    Public Shared Function ComprobarUsuario(ByVal pPrincipal As PrincipalDN, ByVal ws As System.Web.Services.WebService) As PrincipalDN
        Dim wsprincipal As PrincipalDN
        If Not ws.Session Is Nothing Then
            wsprincipal = ws.Session.Item("principal")
            If wsprincipal.guid <> pPrincipal.GUID Then
                Throw New ApplicationException("los principales no coinciden")
            End If
        End If
        If pPrincipal Is Nothing Then
            Throw New ApplicationException("ERROR: Sesion Caducada")
        End If
        Return pPrincipal
    End Function

    Public Shared Function ComprobarUsuario(ByVal ws As System.Web.Services.WebService) As PrincipalDN
        Dim principal As PrincipalDN
        principal = ws.Session.Item("principal")
        If principal Is Nothing Then
            Throw New ApplicationException("ERROR: Sesion Caducada")
        End If
        Return principal
    End Function

    Public Shared Function AsignarUsuario(ByVal ws As System.Web.Services.WebService, ByVal pPrincipal As PrincipalDN) As PrincipalDN
        ' Dim principal As PrincipalDN
        If pPrincipal Is Nothing Then
            Throw New ApplicationException("ERROR: Sesion Caducada")
        End If

        ws.Session.Item("principal") = pPrincipal

        Return pPrincipal
    End Function

End Class
