Imports FN.RiesgosVehiculos.DN
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Public Class PeriodoRenovacionPolizaOidAD




    Public Function RecuperarPeriodoRenovacionPolizaOidDN(ByVal pPago As FN.GestionPagos.DN.PagoDN, ByVal pagoDelPrimerPeridoRenovacion As Boolean) As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "select idpr from vwPagosPrimerPeridoRenovacion  where id=@idpago"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idpago", pPago.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim valorid As Object = ej.EjecutarEscalar(sql, parametros)

            If String.IsNullOrEmpty(valorid) Then
                Throw New Framework.AccesoDatos.ApplicationExceptionAD("no se recupero ningun id de OID para el pago de id" & pPago.ID)
            End If

            Dim id As String
            If Not valorid Is DBNull.Value Then
                id = valorid
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                RecuperarPeriodoRenovacionPolizaOidDN = gi.Recuperar(id, GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN))

            End If



            tr.Confirmar()



        End Using
    End Function

    Public Function Recuperar(ByVal phePeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN) As ColPeriodoRenovacionPolizaOidDN

        ' verificar el tipo


        If phePeriodoRenovacionPoliza Is Nothing Then
            Return Nothing
        End If




        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet

        Using tr As New Transaccion()
            sql = "SELECT     dbo.tlPeriodoRenovacionPolizaOidDN.ID, dbo.tlPeriodoRenovacionPolizaDN.ID ,  dbo.tlPeriodoRenovacionPolizaDN.idPeriodoCoberturaActivo FROM         dbo.tlPeriodoRenovacionPolizaOidDN INNER JOIN                       dbo.tlPeriodoRenovacionPolizaDN ON                       dbo.tlPeriodoRenovacionPolizaOidDN.idPeriodoCobertura = dbo.tlPeriodoRenovacionPolizaDN.idPeriodoCoberturaActivo " & _
                  "    where tlPeriodoRenovacionPolizaDN.ID=@tlPeriodoRenovacionPolizaDNID and dbo.tlPeriodoRenovacionPolizaOidDN.Baja<>@Baja "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("tlPeriodoRenovacionPolizaDNID", phePeriodoRenovacionPoliza.IdEntidadReferida))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim col As New ColPeriodoRenovacionPolizaOidDN


            For Each dr As DataRow In dts.Tables(0).Rows


                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                col.Add(gi.Recuperar(Of PeriodoRenovacionPolizaOidDN)(dr(0)))

            Next


            tr.Confirmar()

            Return col

        End Using

    End Function

    Public Function Recuperar(ByVal pPc As FN.Seguros.Polizas.DN.PeriodoCoberturaDN) As ColPeriodoRenovacionPolizaOidDN


        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet

        Using tr As New Transaccion()
            sql = "Select ID from tlPeriodoRenovacionPolizaOidDN where idPeriodoCobertura=@idPeriodoCobertura and Baja<>@Baja "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idPeriodoCobertura", pPc.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim col As New ColPeriodoRenovacionPolizaOidDN


            For Each dr As DataRow In dts.Tables(0).Rows


                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                col.Add(gi.Recuperar(Of PeriodoRenovacionPolizaOidDN)(dr(0)))

            Next

            'If dts.Tables(0).Rows.Count = 1 Then
            'ElseIf dts.Tables(0).Rows.Count > 1 Then
            '    Throw New ApplicationExceptionAD("Error de integridad en la base de datos: no puede existir perido de cobertura  asignado a más de un PeriodoRenovacionPolizaOidDN ")
            'End If

            tr.Confirmar()

            Return col

        End Using

    End Function



    Public Function RecuperarPagosActivos(ByVal pPeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pImporteDebidoAnulado As Boolean) As FN.GestionPagos.DN.ColPagoDN



        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet

        Using tr As New Transaccion()

            Dim colPago As New FN.GestionPagos.DN.ColPagoDN()

            If pImporteDebidoAnulado Then
                sql = "Select ID from vwPagoxApunteIDxOrigenID where faPago IS  NULL and  faImpD is NOT null and  idPeriodoRenovacionPoliza=@idPeriodoRenovacionPoliza"
            Else
                sql = "Select ID from vwPagoxApunteIDxOrigenID where faPago IS  NULL and faImpD  IS  NULL and  idPeriodoRenovacionPoliza=@idPeriodoRenovacionPoliza"
            End If


            Dim parametro As Int16
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idPeriodoRenovacionPoliza", pPeriodoRenovacionPoliza.ID))

            'For Each guid As String In pPeriodoRenovacionPoliza.ColPeriodosCobertura
            '    parametro += 1
            '    Dim nombreParametro As String = "idPeriodoRenovacionPoliza" & parametro
            '    parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString(nombreParametro, guid))
            '    sql = sql & " or idPeriodoRenovacionPoliza=@" & nombreParametro
            'Next

            '      sql = sql & ")"

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 0 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                For Each fila As System.Data.DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    colPago.Add(gi.Recuperar(Of FN.GestionPagos.DN.PagoDN)(fila(0)))
                Next

            End If

            tr.Confirmar()

            Return colPago

        End Using


    End Function


End Class
