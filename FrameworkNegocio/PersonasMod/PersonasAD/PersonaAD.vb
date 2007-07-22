Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.Personas.DN

Public Class PersonaAD

    Public Function RecuperarPersonaFiscalxNIF(ByVal codigoNif As String) As PersonaFiscalDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim personaF As PersonaFiscalDN = Nothing

        Using tr As New Transaccion()
            If String.IsNullOrEmpty(codigoNif) Then
                Throw New ApplicationException("El identificador fiscal de la persona fiscal no puede ser nulo")
            End If

            sql = "Select ID from vwPersonaFiscal where NIF_Codigo=@codigoNif and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("codigoNif", codigoNif))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una persona fiscal con el mismo NIF")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                personaF = gi.Recuperar(Of PersonaFiscalDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return personaF

        End Using
    End Function

End Class
