Public Class UsuariosGBDAD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


#Region "Constructor"

    Public Sub New(ByVal recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        mRecurso = recurso
    End Sub

#End Region


    Public Overrides Sub CrearTablas()



        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True


        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()

        gbdBase = New Framework.Procesos.ProcesosAD.OperacionesGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()


        gbdBase = New Framework.Notas.NotasAD.NotasGBDAD(mRecurso)
        '     gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()

        gbdBase = New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbdBase.CrearTablas()


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()




        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.DatosIdentidadDN), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.PrincipalDN), Nothing)



    End Sub

    Public Overrides Sub CrearVistas()


        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearVistas()


        gbdBase = New Framework.Procesos.ProcesosAD.OperacionesGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearVistas()

        gbdBase = New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbdBase.CrearVistas()


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwMetodosSistema)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwDatosIdentidad)


    End Sub
End Class
