Imports Framework.LogicaNegocios.Transacciones

Public Class LocalizacionesGBD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


#Region "Constructor"

    Public Sub New(ByVal recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        mRecurso = recurso
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



        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Localizaciones.DN.LocalidadDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(FN.Localizaciones.DN.DireccionNoUnicaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Localizaciones.DN.ContactoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN), Nothing)



    End Sub

    Public Overrides Sub CrearVistas()



        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim ej As Framework.AccesoDatos.Ejecutor

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwLocalidadxCodigoPostal)
    End Sub

#End Region

End Class
