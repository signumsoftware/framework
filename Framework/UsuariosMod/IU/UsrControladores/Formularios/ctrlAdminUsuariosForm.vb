Imports Framework.Usuarios.DN
Imports Framework.Usuarios.IUWin.LNC

Public Class ctrlAdminUsuariosForm
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
        MyBase.New(pNavegador)
    End Sub

#End Region


#Region "Métodos"

    Public Function RecuperarListadoUsuarios() As DataSet
        Dim miLNC As UsuariosLNC
        miLNC = New UsuariosLNC(Me.Marco.DatosMarco)

        Return miLNC.RecuperarListadoUsuarios()
    End Function

    Public Function ObtenerPrincipal(ByVal id As String) As PrincipalDN
        Dim miLNC As UsuariosLNC
        miLNC = New UsuariosLNC(Me.Marco.DatosMarco)

        Return miLNC.ObtenerPrincipal(id)
    End Function

    Public Function GuardarPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN, ByVal nick As String) As PrincipalDN
        Dim miAS As New Framework.Usuarios.IUWin.AS.UsuariosAS()
        Return miAS.GuardarPrincipal(principal, di)
    End Function

    Public Function GuardarPrincipal(ByVal principal As PrincipalDN) As PrincipalDN
        Dim miAS As New Framework.Usuarios.IUWin.AS.UsuariosAS()
        Return miAS.GuardarPrincipal(principal)
    End Function

    Public Function AltaPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim miLNC As New UsuariosLNC(Me.Marco.DatosMarco)
        Return miLNC.AltaPrincipal(principal, di)
    End Function

    Public Function BajaPrincipal(ByVal principal As PrincipalDN) As PrincipalDN
        Dim miLNC As New UsuariosLNC(Me.Marco.DatosMarco)
        Return miLNC.BajaPrincipal(principal)
    End Function

#End Region

End Class