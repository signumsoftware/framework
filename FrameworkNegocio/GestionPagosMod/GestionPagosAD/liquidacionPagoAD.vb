Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos


Public Class LiquidacionPagoAD

    Public Sub RecuperarXPago(ByVal pPago As FN.GestionPagos.DN.PagoDN, ByRef ColLqCompensables As FN.GestionPagos.DN.ColLiquidacionPagoDN, ByRef ColLqAnulable As FN.GestionPagos.DN.ColLiquidacionPagoDN)
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        Using tr As New Transaccion()


            ColLqCompensables = New FN.GestionPagos.DN.ColLiquidacionPagoDN
            ColLqAnulable = New FN.GestionPagos.DN.ColLiquidacionPagoDN


            'sql = "Select [ID],[idpago],[FAnulacion],[idIImporteDebidoDNApunteImpDDN],[Importe],[IdPagoAp],[ImpPagoAp],[femiPagoAp],[faPagoAp] from vwLiquidacionApidPago where idpago=@idpago and FAnulacion=@FAnulacion  "
            'parametros = New List(Of System.Data.IDataParameter)
            'parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idpago", pPago.ID))
            'parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FAnulacion", Date.MinValue))

            ' TODO: revisar esto


            sql = "Select [ID],[idpago],[FAnulacion],[idIImporteDebidoDNApunteImpDDN],[Importe],[IdPagoAp],[ImpPagoAp],[femiPagoAp],[faPagoAp] from vwLiquidacionApidPago where idpago=@idpago   "
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idpago", pPago.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim colLiq As New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each dr In dts.Tables(0).Rows

                ' si no tine fecha de emision bien porque no tenga pago o el pago no haya sido emitido es anulable
                If dr.Item("femiPagoAp") Is DBNull.Value Then
                    ' anulable
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    ColLqAnulable.Add(gi.Recuperar(Of FN.GestionPagos.DN.LiquidacionPagoDN)(dr(0)))
                Else
                    ' compensable  si no esta ya compeada
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    ColLqCompensables.Add(gi.Recuperar(Of FN.GestionPagos.DN.LiquidacionPagoDN)(dr(0)))
                End If
            Next

            tr.Confirmar()





        End Using
    End Sub

    Public Function RecuperarXPago(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        Using tr As New Transaccion()


            sql = "Select id from tlLiquidacionPagoDN where idpago=@idpago  and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idpago", pPago.ID))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim colLiq As New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                colLiq.Add(gi.Recuperar(Of FN.GestionPagos.DN.LiquidacionPagoDN)(dr(0)))
            Next

            tr.Confirmar()



            Return colLiq

        End Using
    End Function


    Public Function RecuperarPagos(ByVal LiquidacionPago As FN.GestionPagos.DN.LiquidacionPagoDN) As FN.GestionPagos.DN.ColPagoDN



        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        Using tr As New Transaccion()


            sql = "Select idPagoAp from vwLiquidacionApidPago where id=@id  "

            parametros = New List(Of System.Data.IDataParameter)
            'parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("id", LiquidacionPago.ID))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim ColPago As New FN.GestionPagos.DN.ColPagoDN

            For Each dr In dts.Tables(0).Rows
                If dr(0) IsNot DBNull.Value Then
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    ColPago.Add(gi.Recuperar(Of FN.GestionPagos.DN.PagoDN)(dr(0)))

                End If
            Next

            tr.Confirmar()



            Return ColPago

        End Using



    End Function

    Public Function RecuperarPagos(ByVal pApunteImpD As FN.GestionPagos.DN.ApunteImpDDN) As FN.GestionPagos.DN.ColPagoDN



        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        Using tr As New Transaccion()


            sql = "Select id from vwPagosOrigenImpDeb where idApunteImpDOrigen=@idApunteImpDOrigen  "

            parametros = New List(Of System.Data.IDataParameter)
            'parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idApunteImpDOrigen", pApunteImpD.ID))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim ColPago As New FN.GestionPagos.DN.ColPagoDN

            For Each dr In dts.Tables(0).Rows
                If dr(0) IsNot DBNull.Value Then
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    ColPago.Add(gi.Recuperar(Of FN.GestionPagos.DN.PagoDN)(dr(0)))

                End If
            Next

            tr.Confirmar()



            Return ColPago

        End Using



    End Function

End Class
