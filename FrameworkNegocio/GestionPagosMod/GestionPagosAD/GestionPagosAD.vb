Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.GestionPagos.DN

Public Class GestionPagosAD



    Public Function RecuperarTipoOrigenxNombre(ByVal tipoOrigen As String) As TipoEntidadOrigenDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim tipoEntOrigen As TipoEntidadOrigenDN = Nothing

        Using tr As New Transaccion()
            If String.IsNullOrEmpty(tipoOrigen) Then
                Throw New ApplicationException("El nombre del tipo de entidad origen no puede ser nulo")
            End If

            sql = "Select ID from tlTipoEntidadOrigenDN where Nombre=@tipoOrigen and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("tipoOrigen", tipoOrigen))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un tipo de origen con el mismo nombre")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                tipoEntOrigen = gi.Recuperar(Of TipoEntidadOrigenDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return tipoEntOrigen

        End Using
    End Function



    Public Function RecuperarGUIDImporteDebidoOrigen(ByVal pGUIDImporteDebido As String) As FN.GestionPagos.DN.ColPagoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim origen As OrigenDN = Nothing

        Using tr As New Transaccion()
            If String.IsNullOrEmpty(pGUIDImporteDebido) Then
                Throw New ApplicationException("La entidad origen no es correcta")
            End If
            Dim colPago As New ColPagoDN()

            sql = "Select ID from tlPagoDN where GUIDIImporteDebidoOrigen=@GUIDIImporteDebidoOrigen"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("GUIDIImporteDebidoOrigen", pGUIDImporteDebido))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colPago.Add(gi.Recuperar(Of PagoDN)(fila(0)))
                Next

            End If

            tr.Confirmar()

            Return colPago

        End Using
    End Function



    Public Function RecuperarOrigenxIdEntidadyTipo(ByVal idEntOrigen As String, ByVal idTipoEntOrigen As String) As OrigenDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim origen As OrigenDN = Nothing

        Using tr As New Transaccion()
            If String.IsNullOrEmpty(idEntOrigen) Then
                Throw New ApplicationException("La entidad origen no es correcta")
            End If

            sql = "Select ID from tlOrigenDN where idTipoEntidadOrigen=@idTipoEntOrigen and IDEntidad=@idEntOrigen and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idTipoEntOrigen", idTipoEntOrigen))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idEntOrigen", idEntOrigen))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un origen del mismo tipo y con igual IdEntidad")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                origen = gi.Recuperar(Of OrigenDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return origen

        End Using
    End Function

    Public Function RecuperarPagoxOrigenxDestinatario(ByVal origen As OrigenDN, ByVal destinatario As FN.Localizaciones.DN.IEntidadFiscalDN) As FN.GestionPagos.DN.ColPagoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim colPago As New ColPagoDN()

        Using tr As New Transaccion()

            sql = "Select ID from vwPagosxOrigenTodos where Baja<>@Baja "

            Dim condicionW As String = ""

            parametros = New List(Of System.Data.IDataParameter)

            If origen IsNot Nothing Then
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idOrigen", origen.ID))
                condicionW = "and idOrigen=@idOrigen  "
            End If

            If destinatario IsNot Nothing Then
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idFiscal", destinatario.IdentificacionFiscal.Codigo))
                condicionW = condicionW & "and idFiscal=@idFiscal "
            End If

            If String.IsNullOrEmpty(condicionW) Then
                Throw New ApplicationExceptionAD("El origen y el destinatario del pago no pueden ser nulos")
            End If

            sql = sql & condicionW

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colPago.Add(gi.Recuperar(Of PagoDN)(fila(0)))
                Next

            End If

            tr.Confirmar()

            Return colPago

        End Using
    End Function

    Public Function RecuperarFicherosTransferenciasActivos() As ColFicheroTransferenciaDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim colFT As New ColFicheroTransferenciaDN()

        Using tr As New Transaccion()
            sql = "Select ID from tlFicheroTransferenciaDN where Baja<>@Baja and FicheroGenerado<>@FicheroGenerado "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("FicheroGenerado", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colFT.Add(gi.Recuperar(Of FicheroTransferenciaDN)(fila(0)))
                Next
            End If

            tr.Confirmar()

            Return colFT

        End Using

    End Function

    Public Function RecuperarFicheroTransferenciasxPago(ByVal pago As PagoDN) As FicheroTransferenciaDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim ft As FicheroTransferenciaDN = Nothing

        Using tr As New Transaccion()
            sql = "Select ID from vwFicheroTransferenciasxPagos where idPago=@idPago and Baja<>@Baja and FicheroGenerado<>@FicheroGenerado "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idPago", pago.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("FicheroGenerado", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(Sql, parametros)

            If dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                ft = gi.Recuperar(Of FicheroTransferenciaDN)(dts.Tables(0).Rows(0)(0))
            ElseIf dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad en la base de datos: no puede existir un pago asignado a más de un fichero")
            End If

            tr.Confirmar()

            Return ft

        End Using

    End Function

    Public Function RecuperarIDPagoCompensador(ByVal ppago As PagoDN) As String
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim ft As FicheroTransferenciaDN = Nothing

        Using tr As New Transaccion()
            sql = "Select id from vwPagosOrigenPago where idPagoCompensado=@idPago  "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idPago", ppago.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            ' parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("FicheroGenerado", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)

            Dim valor As Object
            valor = ej.EjecutarEscalar(sql, parametros)

            If valor Is DBNull.Value Then
                RecuperarIDPagoCompensador = ""
            Else
                RecuperarIDPagoCompensador = valor
            End If

            tr.Confirmar()


        End Using
    End Function

    Public Function RecuperarColPagosMismoOrigenImporteDebido(ByVal pApunteImpD As GestionPagos.DN.ApunteImpDDN) As ColPagoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim ft As FicheroTransferenciaDN = Nothing
        Dim colPago As New ColPagoDN()

        Using tr As New Transaccion()
            ' sql = "Select id from vwPagosOrigenImpDeb where idApunteImpDOrigen=@idApunteImpDOrigen and FechaAnulacion is null and FechaEmision is null and FechaEfecto is null "
            sql = "Select id from vwPagosOrigenImpDeb where idApunteImpDOrigen=@idApunteImpDOrigen   "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idApunteImpDOrigen", pApunteImpD.ID))


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colPago.Add(gi.Recuperar(Of PagoDN)(fila(0)))
                Next

            End If

            RecuperarColPagosMismoOrigenImporteDebido = colPago

            tr.Confirmar()


        End Using
    End Function





    Public Function RecuperarPagosActivosGUIDsOrigenImporteDebidoOrigen(ByVal colGUID As List(Of String), ByVal pImporteDebidoAnulado As Boolean) As ColPagoDN



        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim origen As OrigenDN = Nothing

        Using tr As New Transaccion()

            Dim colPago As New ColPagoDN()

            If pImporteDebidoAnulado Then
                sql = "Select ID from vwPagosOrigenImpDeb where FechaAnulacion IS NOT NULL and  FAnulacion is null and " 'idHuellaIOrigenImpDebDN=@idHuellaIOrigenImpDebDN"
            Else
                sql = "Select ID from vwPagosOrigenImpDeb where FechaAnulacion IS NOT NULL and FAnulacion  IS NOT NULL " ' and idHuellaIOrigenImpDebDN=@idHuellaIOrigenImpDebDN"
            End If


            Dim parametro As Int16
            parametros = New List(Of System.Data.IDataParameter)

            For Each guid As String In colGUID
                parametro += 1
                Dim nombreParametro As String = "idHuellaIOrigenImpDebDN" & parametro
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString(nombreParametro, guid))
                sql = sql & " and idHuellaIOrigenImpDebDN=@" & nombreParametro
            Next


            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colPago.Add(gi.Recuperar(Of PagoDN)(fila(0)))
                Next

            End If

            tr.Confirmar()

            Return colPago

        End Using


    End Function

End Class




