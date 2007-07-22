Imports Framework.LogicaNegocios.Transacciones

Public Class RiesgosVehiculosGBD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


#Region "Constructor"

    Public Sub New(ByVal recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)

        If recurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd=''"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)
        Else
            mRecurso = recurso
        End If



    End Sub

#End Region

#Region "Métodos"

    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True



        Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase

        gbd = New Framework.Cuestionario.CuestionarioAD.CuestionarioGBDAD(mRecurso)
        gbd.CrearTablas()

        gbd = New Framework.Operaciones.OperacionesAD.OperacionesGBD(mRecurso)
        gbd.CrearTablas()

        gbd = New FN.GestionPagos.AD.GestionPagosGBD(mRecurso)
        gbd.CrearTablas()

        gbd = New FN.Seguros.Polizas.AD.PolizasGBD(mRecurso)
        gbd.CrearTablas()

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' operaciones
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.PrimaBaseRVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionImpuestoRVCacheDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionPrimaBaseRVCacheDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionModuladorRVCacheDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionSumaRVCacheDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionFracRVCacheDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.OperacionBonificacionRVCacheDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN), Nothing)




    End Sub

    Public Overrides Sub CrearVistas()



        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        'gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'gbdBase.mRecurso = Me.mRecurso
        'gbdBase.CrearVistas()


        gbdBase = New Framework.Operaciones.OperacionesAD.OperacionesGBD(mRecurso)
        gbdBase.CrearVistas()

        gbdBase = New FN.GestionPagos.AD.GestionPagosGBD(mRecurso)
        gbdBase.CrearVistas()

        gbdBase = New FN.Seguros.Polizas.AD.PolizasGBD(mRecurso)
        gbdBase.CrearVistas()


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwModuladores)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwTarifaImpuestos)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwTarifaPrimasBase)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwTarifaModuladores)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwCategoriaModeloMarca)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagoxApunteIDxOrigenID)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRiesgoMotorXPeridorenovacion)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPresupuestos)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosxPoliza)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPresupuestoVis)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwCajonDocxPresupuesto)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwNotasPresupuesto)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPolizasxPresupuesto)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRiesgoMotoActivos)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwPeriodoRenovacionVisRapida)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwPagosxPolizaSimple)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosxPolizaImpagados)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPolizaVis)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPolizasConImpDebIncidentados)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwDatosPolizaVehiculosXPago)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionesxCobertura)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionesxComision)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionesxImpuesto)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosxLiqXPeridoRenovacion)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwEntidadesColaboradorasIDS)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosPrimerPeridoRenovacion)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwAIDxPCxPR)
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosxAIDXPCxPRxPol)
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionxCausaxAIDxPago)


    End Sub

#End Region

End Class
