Public Class GestionPagosGBD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase


    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)

        If pRecurso Is Nothing Then
            Throw New ApplicationException
        Else
            mRecurso = pRecurso
        End If


        'If pRecurso Is Nothing Then
        '    Dim connectionstring As String
        '    Dim htd As New Generic.Dictionary(Of String, Object)

        '    connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd=''"
        '    htd.Add("connectionstring", connectionstring)
        '    mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        'Else
        '    mRecurso = pRecurso
        'End If
    End Sub

    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True




        '  

        Dim usugbd As New Framework.Usuarios.AD.UsuariosGBDAD(mRecurso)
        usugbd.CrearTablas()

        Dim empresas As New FN.Empresas.AD.EmpresasGBD(mRecurso)
        empresas.CrearTablas()


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        '' empresas
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpresaDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpresaFiscalDN), Nothing)


        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Empresas.DN.DepartamentoNTareaNDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpleadoYPuestosRDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Empresas.DN.SedeEmpresaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.TalonDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidacionPagoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN), Nothing)


        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.FraccionamientoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.PlazoEfectoDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidacionMapDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ParEntFiscalGenericaParamDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LimiteMinFraccionamientoDN), Nothing)


    End Sub

    Public Overrides Sub CrearVistas()



        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True


        'Dim tyrgbd As New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'tyrgbd.mRecurso = Me.mRecurso
        'tyrgbd.CrearVistas()



        Dim empresas As New FN.Empresas.AD.EmpresasGBD(Me.mRecurso)
        empresas.CrearVistas()


        Dim usugbd As New Framework.Usuarios.AD.UsuariosGBDAD(mRecurso)
        usugbd.CrearVistas()




        'Dim pagosgbd As New FN.GestionPagos.AD.GestionPagosGBD(Me.mRecurso)
        'pagosgbd.CrearVistas()



 
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidadorConcretoOrigenIDMapDN)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosOrigenPago)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosOrigenImpDeb)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwApunteImpDebHuellaOrigen)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionAIDPago)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionApidPago)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPagosNoEfectuadosEnFechaEsperada)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwImpDebSumaImportePagos)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwImpDebSumaImportePagosCompenadores)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwImportesDebidosNoCubiertosConPagos)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwImportesDebidosIncidentados)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLiquidacionesXPago)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwAIDProductoxAgrupacion)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwAIDAgrupadosxAgrupacion)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwApunteImpD)

    End Sub
End Class
