Imports Framework.TiposYReflexion.DN
Imports Framework.Procesos.ProcesosDN

Public Class ProcesosHelperLN

#Region "Métodos"

    Public Shared Function AltaTransicion(ByVal colop As ColOperacionDN, ByVal nombreVerboOrigen As String, ByVal nombreVerboDestino As String, ByVal tipoTransicion As TipoTransicionDN, ByVal transAutomatica As Boolean, ByVal metodoGuardaAuto As VinculoMetodoDN, ByVal subordinadaRequerida As Boolean) As TransicionDN
        Dim Transicion As Framework.Procesos.ProcesosDN.TransicionDN

        Transicion = New Framework.Procesos.ProcesosDN.TransicionDN
        Transicion.TipoTransicion = tipoTransicion
        Transicion.OperacionOrigen = colop.RecuperarxNombreVerbo(nombreVerboOrigen)
        Transicion.OperacionDestino = colop.RecuperarxNombreVerbo(nombreVerboDestino)
        Transicion.Automatica = transAutomatica
        Transicion.SubordinadaRequerida = subordinadaRequerida

        If metodoGuardaAuto IsNot Nothing Then
            Transicion.MetodoGuarda = metodoGuardaAuto
        End If

        Return Transicion

    End Function

    Public Shared Function AltaOperacion(ByVal nombreVerbo As String, ByVal colVc As ColVinculoClaseDN, ByVal rutaIco As String, ByVal operacionTrazable As Boolean) As OperacionDN
        Dim verbo As VerboDN
        Dim operacion As OperacionDN

        operacion = New OperacionDN()
        verbo = New VerboDN
        verbo.Nombre = nombreVerbo
        operacion.VerboOperacion = verbo
        operacion.ColDNAceptadas = colVc
        operacion.Nombre = verbo.Nombre
        operacion.RutaIcono = rutaIco
        operacion.TrazarOperacion = operacionTrazable

        Return operacion

    End Function

    Public Shared Function VinculacionVerbo(ByVal colop As ColOperacionDN, ByVal nombreVerbo As String, ByVal vinculoMetodo As VinculoMetodoDN, ByVal ejClienteC As EjecutoresDeClienteDN) As VcEjecutorDeVerboEnClienteDN
        Dim miVerbo As VerboDN
        Dim vc As VcEjecutorDeVerboEnClienteDN

        miVerbo = colop.RecuperarxNombreVerbo(nombreVerbo).VerboOperacion

        vc = New Framework.Procesos.ProcesosDN.VcEjecutorDeVerboEnClienteDN
        vc.ClientedeFachada = ejClienteC.ClientedeFachada
        vc.Verbo = miVerbo
        vc.VinculoMetodo = vinculoMetodo

        Return vc

    End Function

#End Region

End Class