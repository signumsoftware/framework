Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Public Class PolizasAD




    Public Function RecuperarTomador(ByVal identificacionFiscal As String) As FN.Seguros.Polizas.DN.TomadorDN

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "select idTomador from vwTomadorEntidadFiscalGen  where ValorCifNif=@ValorCifNif and Baja=@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("ValorCifNif", identificacionFiscal))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", False))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir mas de una entidad ")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                RecuperarTomador = gi.Recuperar(Of FN.Seguros.Polizas.DN.TomadorDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()


        End Using

    End Function
End Class
