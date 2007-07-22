Imports Framework.Usuarios.DN

Public Class ctrlUsuarioControlador
    Inherits MotorIU.ControlesP.ControladorControlBase

#Region "constructor"

    Public Sub New(ByVal pMotor As MotorIU.Motor.INavegador, ByVal pControl As MotorIU.ControlesP.IControlP)
        MyBase.New(pMotor, pControl)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarColRoles() As ColRolDN
        Dim miLNC As Framework.Usuarios.IUWin.LNC.RolLNC
        miLNC = New Framework.Usuarios.IUWin.LNC.RolLNC(Me.Marco.DatosMarco)

        Return miLNC.RecuperarColRol()
    End Function

#End Region

End Class