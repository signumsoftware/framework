Imports FN.GestionPagos.DN

Public Class frmAdjuntarPagoFTctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Métodos"

    Public Function RecuperarFicherosTransferenciasActivos() As ColFicheroTransferenciaDN
        Dim mias As New FN.GestionPagos.AS.PagosAS()
        Return mias.RecuperarFicherosTransferenciasActivos()
    End Function

    Public Function RecuperarListaPagosxIDs(ByVal listaIDs As ArrayList) As IList
        Dim miAS As New Framework.AS.DatosBasicosAS()
        Return miAS.RecuperarListaTipos(listaIDs, GetType(GestionPagos.DN.PagoDN))
    End Function

    Public Function EjecutarAdjuntarPagoFT(ByRef pPago As FN.GestionPagos.DN.PagoDN, ByRef pMensaje As String) As Boolean
        Try
            Dim mias As New Framework.Procesos.ProcesosAS.OperacionesAS

            Dim mip As Framework.Usuarios.DN.PrincipalDN = Me.Marco.Principal

            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = mip.ColOperaciones.RecuperarxNombreVerbo("Adjuntar Fichero Transferencia")

            Dim mioperacion As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN

            mioperacion.Operacion = op
            mioperacion.SujetoOperacion = mip
            mioperacion.ObjetoIndirectoOperacion = pPago

            pPago = mias.EjecutarOperacionOpr(pPago, mioperacion, Nothing)

            Return True

            'si falla, metemos el error en el mensaje  devolvemos false
        Catch ex As System.Web.Services.Protocols.SoapException
            pMensaje = MotorIU.ExceptionHelper.ConversorExcepcionSoap(ex)
            Return False
        Catch ex As Exception
            pMensaje = ex.Message
            Return False
        End Try

    End Function

#End Region

End Class
