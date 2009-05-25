Public Class frmAdministracionEstadoDocumentosctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Function ProcesarCambioEstadoOperacion(ByVal pListaIds As List(Of String), ByVal pEstado As AmvDocumentosDN.EstadosRelacionENFichero) As Object
        Dim miColComandoOperacion As New AmvDocumentosDN.ColComandoOperacionDN

        If Not pListaIds Is Nothing Then
            For a As Integer = 0 To pListaIds.Count - 1
                Dim micomando As New AmvDocumentosDN.ComandoOperacionDN
                micomando.IDRelacion = pListaIds(a)
                micomando.EstadoSolicitado = pEstado
                miColComandoOperacion.Add(micomando)
            Next

            Dim mias As New ClienteAS.ClienteAS
            Return mias.ProcesarColComandooperacion(miColComandoOperacion)
        End If

        Return Nothing

    End Function

End Class
