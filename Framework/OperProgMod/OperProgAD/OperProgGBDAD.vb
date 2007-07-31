Public Class OperProgGBDAD
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


        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase
        gbdBase = New Framework.Notas.NotasAD.NotasGBDAD(mRecurso)
        gbdBase.CrearTablas()

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.OperProg.OperProgDN.AlertaDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()
        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True


        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase



        gbdBase = New Framework.Notas.NotasAD.NotasGBDAD(mRecurso)
        gbdBase.CrearVistas()


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)


        ej.EjecutarNoConsulta(My.Resources.vwAlertasXHEDN)





    End Sub
End Class
