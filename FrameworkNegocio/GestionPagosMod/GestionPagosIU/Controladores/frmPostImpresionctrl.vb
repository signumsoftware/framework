Public Class frmPostImpresionctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Function ValidarImpresionTalon(ByRef pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.PagoDN

        Dim mias As New Framework.Procesos.ProcesosAS.OperacionesAS

        Dim mip As Framework.Usuarios.DN.PrincipalDN = Me.Marco.Principal

        Dim op As Framework.Procesos.ProcesosDN.OperacionDN = mip.ColOperaciones.RecuperarxNombreVerbo("Validación Impresión")

        Dim mioperacion As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN

        mioperacion.Operacion = op
        mioperacion.SujetoOperacion = mip
        mioperacion.ObjetoIndirectoOperacion = pPago

        pPago = mias.EjecutarOperacionOpr(pPago, mioperacion, Nothing)

        Return pPago

    End Function

    Public Function AnularImpresionTalon(ByRef pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.PagoDN

        Dim mias As New Framework.Procesos.ProcesosAS.OperacionesAS

        Dim mip As Framework.Usuarios.DN.PrincipalDN = Me.Marco.Principal

        Dim op As Framework.Procesos.ProcesosDN.OperacionDN = mip.ColOperaciones.RecuperarxNombreVerbo("Anulación Impresión")

        Dim mioperacion As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN

        mioperacion.Operacion = op
        mioperacion.SujetoOperacion = mip
        mioperacion.ObjetoIndirectoOperacion = pPago

        pPago = mias.EjecutarOperacionOpr(pPago, mioperacion, Nothing)

        Return pPago


    End Function
End Class
