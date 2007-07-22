Public Class tiposYReflexionGBDAD

    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean



    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True



        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.TiposYReflexion.DN.VinculoMetodoDN), Nothing)





    End Sub

    Public Overrides Sub CrearVistas()
        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwVinculosMetodo)

    End Sub

    ' Public Overrides Sub RegistrarNavegacionEnsamblado()
    'Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
    'ln.RegistrarEnsamblado(Me.GetType.Assembly)
    'End Sub
End Class
