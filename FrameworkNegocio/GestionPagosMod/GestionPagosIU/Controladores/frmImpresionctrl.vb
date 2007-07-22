Public Class frmImpresionctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase


    Public Function AutorizarImpresionTalon(ByRef pPago As FN.GestionPagos.DN.PagoDN, ByRef pMensaje As String) As Boolean

        Try
            Dim mias As New Framework.Procesos.ProcesosAS.OperacionesAS

            Dim mip As Framework.Usuarios.DN.PrincipalDN = Me.Marco.Principal

            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = mip.ColOperaciones.RecuperarxNombreVerbo("Impresión")

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
End Class
