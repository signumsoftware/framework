''' <summary>
''' clase encargada de generar las vistas procedimientos almacenados y demas que sean necesarios
''' </summary>
''' <remarks></remarks>

Public Class OperacionesGBD




    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Implements IGBD


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

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' operaciones

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Operaciones.OperacionesDN.SumValOperMapDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Operaciones.OperacionesDN.SumiValFijoDN), Nothing)



    End Sub

    Public Function RecuperarColComandos() As System.Collections.Generic.List(Of String) Implements IGBD.RecuperarColComandos

    End Function

    Public Overrides Sub CrearVistas()
        'vwOperacionesRealizadasActivas
    End Sub
End Class



Public Interface IGBD
    Function RecuperarColComandos() As List(Of String)
End Interface