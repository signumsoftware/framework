Public Class GestorSalidaGBD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase


    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean



    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        mRecurso = pRecurso
    End Sub

    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.GestorSalida.DN.DocumentoSalida), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.GestorSalida.DN.CategoriaImpresoras), Nothing)
    End Sub

    Public Overrides Sub CrearVistas()
        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim vwDocumentosSalidaMailEnCola As String = My.Resources.vwDocumentosSalidaMailEnCola
        Dim vwDocumentosSalidaFaxEnCola As String = My.Resources.vwDocumentosSalidaFaxEnCola
        Dim vwDocumentosSalidaImpresoraEnCola As String = My.Resources.vwDocumentosSalidaImpresionEnCola
        Dim vw1 As String = My.Resources.vwRepositoriosPersistentesDisponibles
        Dim vw2 As String = My.Resources.vwRepositoriosTemporalesDisponibles

        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(vwDocumentosSalidaMailEnCola)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(vwDocumentosSalidaFaxEnCola)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(vwDocumentosSalidaImpresoraEnCola)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(vw1)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(vw2)
    End Sub
End Class
