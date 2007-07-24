Imports Framework.LogicaNegocios.Transacciones


Public Class AdaptadorInformesQueryBuildingGBD

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

        'Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        'gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'gbdBase.mRecurso = mRecurso
        'gbdBase.CrearTablas()


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()
        'no hay vistas que generar

        'Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase
        'gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'gbdBase.mRecurso = Me.mRecurso
        'gbdBase.CrearVistas()


        'Dim ej As Framework.AccesoDatos.Ejecutor

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwImpresionTarifa1)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwImpresionTarifa2)
    End Sub




End Class