Imports Framework.Usuarios.DN
Imports Framework.Usuarios.IUWin.AS

Public Class ctrlAdminPermisosForm
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
        MyBase.New(pNavegador)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarColRol() As ColRolDN
        Dim miAS As New UsuariosAS()
        Return miAS.RecuperarColRol()
    End Function

    Public Function RecuperarListaCasosUso() As IList(Of CasosUsoDN)
        Dim miAS As New UsuariosAS()
        Return miAS.RecuperarListaCasosUso()
    End Function

    Public Function RecuperarMetodos() As IList(Of MetodoSistemaDN)
        Dim miAS As New UsuariosAS()
        Return miAS.RecuperarMetodos()
    End Function

    Public Function GuardarRol(ByVal rol As RolDN)
        Dim miAS As New UsuariosAS()
        Return miAS.GuardarRol(rol)
    End Function

    Public Function GuardarCasoUso(ByVal casoUso As CasosUsoDN)
        Dim miAS As New UsuariosAS()
        Return miAS.GuardarCasoUso(casoUso)
    End Function

#End Region

End Class
