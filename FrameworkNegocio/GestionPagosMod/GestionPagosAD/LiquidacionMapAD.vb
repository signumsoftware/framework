
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos


Public Class LiquidacionMapAD



    Public Function Recuperar(ByVal pCodGrupoLiquidacion As String) As FN.GestionPagos.DN.ColLiquidacionMapDN


        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet


        Using tr As New Transaccion()


            sql = " Select id from tlLiquidacionMapDN where CodGrupoLiquidacion=@CodGrupoLiquidacion  "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("CodGrupoLiquidacion", pCodGrupoLiquidacion))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim ColLiq As New FN.GestionPagos.DN.ColLiquidacionMapDN

            For Each dr In dts.Tables(0).Rows
                If dr(0) IsNot DBNull.Value Then
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    ColLiq.Add(gi.Recuperar(Of FN.GestionPagos.DN.LiquidacionMapDN)(dr(0)))
                End If
            Next

            tr.Confirmar()

            Return ColLiq

        End Using


    End Function

End Class
