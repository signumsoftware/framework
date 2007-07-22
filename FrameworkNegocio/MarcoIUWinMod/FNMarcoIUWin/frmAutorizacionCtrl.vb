Public Class frmAutorizacionCtrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    'constructor vacío para levantar el ensamblado de manera dinámica
    Public Sub New()

    End Sub

#Region "metodos"
    Public Function LogarseEnSistema(ByVal pNombre As String, ByVal pClave As String) As Boolean
        Dim di As New Framework.Usuarios.DN.DatosIdentidadDN(pNombre, pClave)

        Dim urs As Framework.Usuarios.IUWin.AS.UsuariosAS = New Framework.Usuarios.IUWin.AS.UsuariosAS

        Dim miprincipal As Framework.Usuarios.DN.PrincipalDN = urs.IniciarSesion(di)

        If Not miprincipal Is Nothing Then
            If Me.Marco.DatosMarco.Contains("Principal") Then
                Me.Marco.DatosMarco.Item("Principal") = miprincipal
            Else
                Me.Marco.DatosMarco.Add("Principal", miprincipal)
            End If

            Return True
        Else
            Return False
        End If
    End Function
#End Region

End Class
