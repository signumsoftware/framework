Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos



Public Class TarificadorGBD

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


        Dim gbd As New Framework.Operaciones.OperacionesAD.OperacionesGBD(mRecurso)
        gbd.CrearTablas()

        Dim gbd2 As New Framework.Cuestionario.CuestionarioAD.CuestionarioGBDAD(mRecurso)
        gbd2.CrearTablas()



        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        ' operaciones

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Tarificador.TarificadorDN.SumiValCaracteristicaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Tarificador.TarificadorDN.TraductorxMapMemoriaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN), Nothing)



    End Sub


    Public Overrides Sub CrearVistas()
        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True
    End Sub

End Class
