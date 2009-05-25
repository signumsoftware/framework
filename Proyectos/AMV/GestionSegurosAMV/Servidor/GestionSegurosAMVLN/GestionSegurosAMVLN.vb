Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Cuestionario.CuestionarioDN

Imports FN.Seguros.Polizas.DN

Public Class GestionSegurosAMVLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

#Region "Métodos"

    'Public Function TarificarPresupuesto(ByVal presupuesto As PresupuestoDN) As PresupuestoDN

    '    Using tr As New Transaccion()
    '        Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("OperacionConfigurada"), Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN)
    '        If opc Is Nothing Then
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se ha podido recuperar la operación configurada del grafo de tarificación actual")
    '        End If

    '        Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
    '        If opc.IOperacionDN.IRecSumiValorLN Is Nothing Then
    '            irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN()
    '        Else
    '            irec = opc.IOperacionDN.IRecSumiValorLN
    '        End If

    '        irec.Tarifa = presupuesto.Tarifa
    '        irec.DataSoucers.Add(irec.Tarifa)

    '        Dim dTarifaV As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN
    '        dTarifaV = CType((presupuesto.Tarifa).DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN) '.Clone()

    '        If dTarifaV.HeCuestionarioResuelto.EntidadReferida Is Nothing Then
    '            'Habría que recuperarlo de la base de datos
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No hay un cuestionario resuelto")
    '        End If
    '        irec.DataSoucers.Add(dTarifaV.HeCuestionarioResuelto.EntidadReferida)

    '        opc.IOperacionDN.IRecSumiValorLN = irec

    '        Dim valor As Double = opc.IOperacionDN.GetValor()

    '        'Actualizamos los valores de tarifa

    '        dTarifaV.AsignarResultadosTarifa(irec.RecuperarColOpImpuestos(), irec.RecuperarColOpModulador(), irec.RecuperarColOpPB(), irec.RecuperarColOpSuma())

    '        dTarifaV.Importe = valor

    '        'irec.DataResults.Clear()
    '        'irec.DataSoucers.Clear()
    '        'irec.Tarifa = Nothing
    '        'irec = Nothing
    '        'opc.IOperacionDN.IRecSumiValorLN = Nothing
    '        'dTarifaV.DesRegistrarTodo()
    '        'presupuesto.Tarifa.DesRegistrarTodo()
    '        'presupuesto.DesRegistrarTodo()
    '        'dTarifaV.EliminarEntidadesOReferidasOpCache()
    '        'dTarifaV.HeCuestionarioResuelto.EliminarEntidadReferida()
    '        'opc = Nothing

    '        'se guarda y se recupera el presupuesto para garantizar que no tiene referenciadas más entidades de las necesarias
    '        irec.ClearAll()

    '        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
    '        gi.Guardar(presupuesto)

    '        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
    '        Dim presupuestoBD As PresupuestoDN = gi.Recuperar(Of PresupuestoDN)(presupuesto.ID)

    '        tr.Confirmar()

    '        Return presupuestoBD

    '    End Using

    'End Function

    'Public Sub CargarGrafoTarificacion()
    '    Using tr As New Transaccion()
    '        'Cargamos en memoria el grafo de tarificación
    '        'TODO: Habría que cargar el grafo actual por la fecha
    '        Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
    '        Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
    '        opc = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)
    '        If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("OperacionConfigurada") Is Nothing Then
    '            Framework.Configuracion.AppConfiguracion.DatosConfig.Add("OperacionConfigurada", opc)
    '        Else
    '            Framework.Configuracion.AppConfiguracion.DatosConfig.Item("OperacionConfigurada") = opc
    '        End If


    '        tr.Confirmar()

    '    End Using
    'End Sub

#End Region

End Class
