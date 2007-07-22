Public Class EmpresasGBD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean



    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        If pRecurso Is Nothing Then
            'Dim connectionstring As String
            'Dim htd As New Generic.Dictionary(Of String, Object)

            'connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd=''"
            'htd.Add("connectionstring", connectionstring)
            'mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)
            Throw New ApplicationException
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
        Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase

        gbd = New FN.Localizaciones.AD.LocalizacionesGBD(mRecurso)
        gbd.CrearTablas()

        ' empresas
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpresaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpresaFiscalDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.DepartamentoNTareaNDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpleadoYPuestosRDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.SedeEmpresaDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.AgrupacionDeEmpresasDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EntidadColaboradoraDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()



        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True


        Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase

        gbd = New FN.Localizaciones.AD.LocalizacionesGBD(Me.mRecurso)
        gbd.CrearVistas()

        gbd = New FN.Personas.AD.PersonasGBD(Me.mRecurso)
        gbd.CrearVistas()



        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwSedexEmpresa)




    End Sub
End Class
