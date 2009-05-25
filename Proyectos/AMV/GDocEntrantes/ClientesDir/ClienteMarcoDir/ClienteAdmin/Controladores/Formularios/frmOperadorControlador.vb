Imports AmvDocumentosDN

Public Class frmOperadorControlador
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

#End Region

#Region "Métodos"

    Public Sub GuardarOperador(ByVal operador As AmvDocumentosDN.OperadorDN)
        Dim miLNC As New ClienteAdminLNC.OperadorLNC(Me.Marco.DatosMarco)
        miLNC.GuardarOperador(operador)
    End Sub

    Public Function RecuperarListaOperador() As IList(Of AmvDocumentosDN.OperadorDN)
        Dim miLNC As New ClienteAdminLNC.OperadorLNC(Me.Marco.DatosMarco)
        Return miLNC.RecuperarListaOperador()
    End Function

    Public Function RecuperarOperador(ByVal id As String) As OperadorDN
        Dim miLNC As New ClienteAdminLNC.OperadorLNC(Me.Marco.DatosMarco)
        Return miLNC.RecuperarOperador(id)
    End Function

    Public Sub BajaOperador(ByVal idOperador As String)
        Dim miLNC As New ClienteAdminLNC.OperadorLNC(Marco.DatosMarco)
        miLNC.BajaOperador(idOperador)
    End Sub

    Public Sub ReactivarOperador(ByVal idOperador As String)
        Dim miLNC As New ClienteAdminLNC.OperadorLNC(Marco.DatosMarco)
        miLNC.ReactivarOperador(idOperador)
    End Sub
#End Region

End Class
