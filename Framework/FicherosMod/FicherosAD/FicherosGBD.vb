Public Class FicherosGBD
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



        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(Framework.Ficheros.FicherosDN.MapeadoDocumentoEntidadDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Ficheros.FicherosDN.CajonDocumentoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()



        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)


        ej.EjecutarNoConsulta(My.Resources.uwCajonDocumentoxTipodocXEntidadReferida)

        ej.EjecutarNoConsulta(My.Resources.uwTipoDocRequeridoXEntidadRef)

        'ej.EjecutarNoConsulta(My.Resources.vwCajonDocxIdentidadDoc)

        ej.EjecutarNoConsulta(My.Resources.vwCDyHFBienVincualdos)
        ej.EjecutarNoConsulta(My.Resources.vwCajonDocumentoDNVincualdos)
        ej.EjecutarNoConsulta(My.Resources.vwCDyHFMalVincualdos)
        ej.EjecutarNoConsulta(My.Resources.vwCDIdentificadosNoVinculados)
        ej.EjecutarNoConsulta(My.Resources.vwHuellaFicheroIdentificados)
        ej.EjecutarNoConsulta(My.Resources.vwHuellaFicheroIdentificadosNoVinculados)

        ej.EjecutarNoConsulta(My.Resources.vwCajonDocumentoVis)
        ej.EjecutarNoConsulta(My.Resources.vwHuellaFicheroVis)
        ej.EjecutarNoConsulta(My.Resources.vwHuellaFicheroIdentificadosVis)
        ej.EjecutarNoConsulta(My.Resources.vwCDyHFVinculables)

        '        ej.EjecutarNoConsulta(My.Resources.vwHuellaFicheroVis)

        ej.EjecutarNoConsulta(My.Resources.vwCajonDocVerificablesxEntidadRef)



    End Sub
End Class
