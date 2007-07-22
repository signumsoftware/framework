Public Class NotasGBDAD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase


    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)


        If pRecurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd=''"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Else
            mRecurso = pRecurso
        End If


    End Sub

    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Notas.NotasDN.NotaDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()


        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)


        ej.EjecutarNoConsulta(My.Resources.vwNotasNotasGUIDentRef)





    End Sub
End Class
