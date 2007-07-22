Public Class frmArchivoctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub

#Region "métodos"
    Public Function RecuperarRelacionEnFichero(ByVal pID As String) As AmvDocumentosDN.RelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS

        Return mias.RecuperarRelacionEnFichero(pID)

    End Function

    Public Function RecuperarOperacionActivaPorIdEntidad(ByVal pIDEntidadNegocio As String, ByRef mensaje As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS

        Return mias.RecuperarOperacionActivaPorIdEntidad(pIDEntidadNegocio, mensaje)
    End Function

#End Region
End Class
