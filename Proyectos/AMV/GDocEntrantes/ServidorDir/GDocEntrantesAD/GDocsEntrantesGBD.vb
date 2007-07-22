Imports Framework.LogicaNegocios.Transacciones

Public Class GDocsEntrantesGBD
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

        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase


        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()
        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()
        gbdBase = New Framework.Usuarios.AD.UsuariosGBDAD(Me.mRecurso)
        gbdBase.CrearTablas()

        'gbd = New Framework.Cuestionario.CuestionarioAD.CuestionarioGBDAD(mRecurso)
        'gbd.CrearTablas()

        'gbd = New Framework.Operaciones.OperacionesAD.OperacionesGBD(mRecurso)
        'gbd.CrearTablas()

        'gbd = New FN.GestionPagos.AD.GestionPagosGBD(mRecurso)
        'gbd.CrearTablas()

        'gbd = New FN.Seguros.Polizas.AD.PolizasGBD(mRecurso)
        'gbd.CrearTablas()

        'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        '' operaciones
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN), Nothing)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' procesos

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Personas.DN.PersonaDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.TransicionRealizadaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.OperacionEnRelacionENFicheroDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.OperadorDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.HuellaOperadorDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.CanalEntradaDocsDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.RelacionENFicheroDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.DatosFicheroIncidentado), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.PrincipalDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.DatosIdentidadDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.SobreBasicoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.NotificacionDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.SuscripcionDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMails.DN.SobreDN), Nothing)

        'Tablas Gestión de pagos
        '_________________________________________________________________________________


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ApunteImpDDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        'gi.GenerarTablas2(GetType(FN.GestionPagos.DN.OrigenIdevManualDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidacionPagoDN), Nothing)




        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.PagoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.NotificacionPagoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LimitePagoDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ContenedorRTFDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ReemplazosTextoCartasDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.PagoTrazaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN), Nothing)

        '_________________________________________________________________________________

        'Tablas EmpresaDN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpleadoYPuestosRDN), Nothing)


        '_________________________________________________________________________________



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

        gbdBase = New Framework.Usuarios.AD.UsuariosGBDAD(Me.mRecurso)
        gbdBase.CrearVistas()



        Dim ej As Framework.AccesoDatos.Ejecutor


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesEstadoCanal)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesENFSinOperador)


        'dependientes de tablas -------------





        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vuOperaciones)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwNumDocPendientesClasificacionXTipoCanal)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwNumDocPendientesPostClasificacionXTipoEntidadNegocio)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesREN)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwPrincipalxEntidadUser)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelEntFicheroSel)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUsuariosOperadores)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesPendientes)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesAClasificar)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwDatosIdentidad)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUsuariosxEntidadRef)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperadoresVis)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwSiguienteCorreo)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwListaMailsAdmin)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUsuariosxEntidadRefSel)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesENF)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vuOperacionesAsignadasXOperador)




        '----------------------------
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesAsignadasOperador)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarEntrada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperar)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarxTipoEntrada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarRechazadaEntrada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarNoRechazadaEntrada)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridadNoREntrada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridadREntrada)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridadEntrada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarxTipo)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarRechazada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionxRecuperarNoRechazada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridadNoR)
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridadR)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxRecuperarPrioridad)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesAPostClasificar)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwOpPostClasificarSinCanal)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionPostClasificarxTipo)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionPostClasificarRechazada)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimaOperacionPostClasificarNoRechazada)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesAPostClasificarPrioridadR)




        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesEstadoFicheroCerrado)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesNoCerradasTiempoExcedido)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUnificacionEstadoOperacionesFichero)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwTrazaOperacionesVis)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesAPostClasificarPrioridadNoR)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesAPostClasificarPrioridad)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vuVerificacionOperacionesPostClasificar)



        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwBuscarFicherosClienteVis)




        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwOpRechazadasSinCanal)






    End Sub

#End Region

End Class
