Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos

Public Class MapIorigenImpDevLiquidadoresAD


    Public Function Recuperar(ByVal pTipoOrigen As System.Type, ByVal pFechaConsulta As Date) As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        Using tr As New Transaccion()


            sql = "Select ID from vwLiquidadorConcretoOrigenIDMapDN where NombreClaseOrigen=@NombreClaseOrigen and  Periodo_FInicio<=@Periodo_FInicio and (Periodo_FFinal is null or Periodo_FFinal>=@Periodo_FFinal ) and Baja<>@Baja"
            '  sql = "Select ID from vwtlLiquidadorConcretoOrigenIDMapDN where NombreClaseOrigen=@NombreClaseOrigen and  Periodo_FInicio>=@Periodo_FInicio  and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreClaseOrigen", pTipoOrigen.FullName))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("Periodo_FInicio", pFechaConsulta))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("Periodo_FFinal", pFechaConsulta))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un Mapeado vigente para el mismo origen de importe debido")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                map = gi.Recuperar(Of FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN)(dts.Tables(0).Rows(0)(0))

            End If

            tr.Confirmar()

            Return map

        End Using
    End Function
End Class
