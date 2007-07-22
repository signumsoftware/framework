Imports Framework.LogicaNegocios.Transacciones

Public Class TrabajosGBD
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



        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' Trabajos
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Trabajos.DN.AsignacionTrabajoDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()


        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True
        'Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        'gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'gbdBase.mRecurso = Me.mRecurso
        'gbdBase.CrearVistas()



        'Dim ej As Framework.AccesoDatos.Ejecutor
        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.)

    End Sub

#End Region

End Class
