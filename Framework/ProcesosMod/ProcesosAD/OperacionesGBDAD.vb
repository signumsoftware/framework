Public Class OperacionesGBDAD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean



    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True

        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()





        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.TransicionRealizadaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.HistOperacionRealizadaDN), Nothing)






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


        'gbdBase = New Framework.Usuarios.AD.UsuariosGBDAD
        'gbdBase.mRecurso = Me.mRecurso
        'gbdBase.CrearVistas()






        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwTransicionesxTipoDN)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwOperacionesRealizadasActivas)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwEjecutorClientexNombreCliente)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwProcesosTransicionesOrDes)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwProcesosTrOprOrigenOprDestino)


        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimasOperacionesTRNormal)
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimasOperacionesSubordinadas)
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwUltimasOperacionesTotales)



    End Sub
End Class
