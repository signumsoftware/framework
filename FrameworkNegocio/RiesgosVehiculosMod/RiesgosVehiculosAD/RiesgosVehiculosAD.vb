Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.RiesgosVehiculos.DN
Imports FN.Seguros.Polizas.DN

Public Class RiesgosVehiculosAD



    Public Function RecuperarTarifasRefierenCDs(ByVal pColCajonDocumento As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN) As FN.Seguros.Polizas.DN.ColTarifaDN

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim RiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN = Nothing
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            Dim condiciones As String

            sql = "select idTarifa from vwCDxTarifa where  "

            parametros = New List(Of System.Data.IDataParameter)
            Framework.AccesoDatos.ParametrosHelperAD.ProcesarColEntidadesBase(pColCajonDocumento, "id", condiciones, 1, parametros)

            sql += condiciones.Substring(0, condiciones.Length - 4)



            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)



            Dim col As New FN.Seguros.Polizas.DN.ColTarifaDN

            For Each dr As DataRow In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                col.Add(gi.Recuperar(dr.Item(0).ToString(), GetType(FN.Seguros.Polizas.DN.TarifaDN)))
            Next

            tr.Confirmar()

            Return col

        End Using

    End Function

    Public Function RecuperarPagos(ByVal idPeridoRenovacionOrigenImporte As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN, ByVal identidadfiscalAcreedora As String) As FN.GestionPagos.DN.ColPagoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim RiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN = Nothing
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "select id from vwPagosxLiqXPeridoRenovacion where idOrigenAID=@idOrigenAID and idAcreedora=@idAcreedora  "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idAcreedora", identidadfiscalAcreedora))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idOrigenAID", idPeridoRenovacionOrigenImporte.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)



            Dim col As New FN.GestionPagos.DN.ColPagoDN

            For Each dr As DataRow In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                col.Add(gi.Recuperar(dr.Item(0).ToString(), GetType(FN.GestionPagos.DN.PagoDN)))
            Next

            tr.Confirmar()

            Return col

        End Using
    End Function

    Public Function RecuperarRiesgoMotorActivo(ByVal pMatricula As String, ByVal pNumeroBastidor As String) As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim RiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN = Nothing
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            '  sql = "select id from vwRiesgoMotoActivos where (NumeroBastidor=@NumeroBastidor or ValorMatricula=@ValorMatricula)  and Baja<>@Baja"
            sql = "select id from vwRiesgoMotoActivos where (NumeroBastidor=@NumeroBastidor or ValorMatricula=@ValorMatricula)  "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NumeroBastidor", pNumeroBastidor))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("ValorMatricula", pMatricula))
            '   parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos,no debiera exitir mas de un riesgo motor")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                RiesgoMotor = gi.Recuperar(Of FN.RiesgosVehiculos.DN.RiesgoMotorDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return RiesgoMotor

        End Using
    End Function

    Public Function RecuperarModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal matriculada As Boolean, ByVal fecha As Date) As ModeloDatosDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim modeloDatos As ModeloDatosDN = Nothing
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "select * from vwCategoriaModeloMarca where Modelo=@nombreModelo and Marca=@nombreMarca and Matriculado=@matriculada and FIModeloDatos<=@fecha and (FFModeloDatos>=@fecha or FFModeloDatos is null)  and BajaModeloDatos=@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("nombreModelo", nombreModelo))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("nombreMarca", nombreMarca))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("matriculada", matriculada))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("fecha", fecha))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", False))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir el mismo modelo, marca y estado matriculación")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                modeloDatos = gi.Recuperar(Of ModeloDatosDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return modeloDatos

        End Using

    End Function

    Public Function RecuperarHuellasPeridosRenovacionActivosParaRiesgoMotor(ByVal pNumeroMatricula As String, ByVal pNumeroBastidor As String) As IList(Of FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN)


        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim modeloDatos As ModeloDatosDN = Nothing
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As Data.DataSet
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "select idpr,guidPR  from vwRiesgoMotorXPeridorenovacion  where valorMatricula=@valorMatricula or NumeroBastidor=@NumeroBastidor and Baja=@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("valorMatricula", pNumeroMatricula))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NumeroBastidor", pNumeroBastidor))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", False))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim col As New List(Of FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN)

            For Each dr As DataRow In dts.Tables(0).Rows

                Dim hpr As New FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN

                hpr.AsignarDatosBasicos(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN), dr(0), dr(1))

                col.Add(hpr)
            Next


            tr.Confirmar()

            Return col

        End Using


    End Function

    Public Function RecuperarModelosPorMarca(ByVal pMarca As MarcaDN) As List(Of ModeloDN)
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim ds As DataSet
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()
            sql = "select ID from tlModeloDN  where idMarca=@idmarca"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idmarca", pMarca.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            ds = ej.EjecutarDataSet(sql, parametros)

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            Dim lista As New List(Of ModeloDN)

            For Each dr As DataRow In ds.Tables(0).Rows
                lista.Add(gi.Recuperar(dr.Item(0).ToString(), GetType(ModeloDN)))
            Next

            tr.Confirmar()

            Return lista

        End Using

    End Function

    Public Function RecuperarCategoria(ByVal modelo As ModeloDN, ByVal matriculada As Boolean) As CategoriaDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim dts As DataSet
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim categoria As CategoriaDN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()
            sql = "select idCategoria,GUIDCategoria from vwCategoriaModeloMarca where idModelo=@idModelo and Matriculado=@Matriculado"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idModelo", modelo.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Matriculado", matriculada))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)


            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una categoría para el mismo modelo, y estado matriculación")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                categoria = gi.Recuperar(dts.Tables(0).Rows(0)("idCategoria"), GetType(CategoriaDN))
            End If

            tr.Confirmar()

            Return categoria

        End Using
    End Function

    Public Function RecuperarHuellaCategoria(ByVal modelo As ModeloDN, ByVal matriculada As Boolean) As HECategoriaModDatosDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim dts As DataSet
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim heCat As HECategoriaModDatosDN = Nothing

        Using tr As New Transaccion()
            sql = "select distinct tlCategoriaModDatosDN.ID,tlCategoriaModDatosDN.GUID from tlCategoriaModDatosDN " & _
                    "inner join trtlCategoriaModDatosDNColModelosDatosXtlModeloDatosDN on idttlCategoriaModDatosDN=tlCategoriaModDatosDN.ID " & _
                    "inner join tlModeloDatosDN on tlModeloDatosDN.ID=idptlModeloDatosDN " & _
                    "where idModelo=@idModelo and Matriculado=@Matriculado"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idModelo", modelo.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Matriculado", matriculada))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)


            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una categoría para el mismo modelo, y estado matriculación")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                heCat = New HECategoriaModDatosDN()
                heCat.AsignarDatosBasicos(GetType(FN.RiesgosVehiculos.DN.CategoriaModDatosDN), dts.Tables(0).Rows(0)("ID"), dts.Tables(0).Rows(0)("GUID"))
            End If

            tr.Confirmar()

            Return heCat

        End Using
    End Function

    Public Function RecuperarDatosTarifaVehiculosDN(ByVal pago As FN.GestionPagos.DN.PagoDN) As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim miID As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()

            sql = "select idDatosPolizaVehiculos from vwDatosPolizaVehiculosXPago  where idPago=@idPago"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idPago", pago.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            miID = ej.EjecutarEscalar(sql, parametros)

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            RecuperarDatosTarifaVehiculosDN = gi.Recuperar(miID, GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN))

            tr.Confirmar()

        End Using

    End Function

    Public Function RecuperarProductosModelo(ByVal categoria As CategoriaDN, ByVal fecha As Date) As ColProductoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim ds As DataSet
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()
            sql = "select distinct tlProductoDN.ID from tlPrimaBaseRVDN " & _
                    "inner join tlCoberturaDN on idCobertura=tlCoberturaDN.ID " & _
                    "inner join trtlProductoDNColCoberturasXtlCoberturaDN on idptlCoberturaDN=tlCoberturaDN.ID " & _
                    "inner join tlProductoDN on idttlProductoDN=tlProductoDN.ID " & _
                    "where Periodo_FInicio<=@Fecha and (Periodo_FFinal>=@Fecha or Periodo_FFinal is null) and idCategoria=@idCategoria"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("Fecha", fecha))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idCategoria", categoria.ID))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            ds = ej.EjecutarDataSet(sql, parametros)

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            Dim colProducto As New ColProductoDN

            For Each dr As DataRow In ds.Tables(0).Rows
                colProducto.Add(gi.Recuperar(dr.Item(0).ToString(), GetType(ProductoDN)))
            Next

            tr.Confirmar()

            Return colProducto
        End Using

    End Function

    Public Function CalcularNivelBonificacion(ByVal valorBonificacion As Double, ByVal categoria As CategoriaDN, ByVal bonificacion As BonificacionDN, ByVal fecha As Date) As String
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim dts As DataSet
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim nivelBonificacion As String = String.Empty

        Using tr As New Transaccion()
            sql = "select Nombre from tlBonificacionRVDN where IntervaloNumerico_ValInf<=@ValorBonificacion and IntervaloNumerico_ValSup>=@ValorBonificacion " & _
                    "and idCategoria=@idCategoria and idBonificacion=@idBonificacion and periodo_FInicio<=@Fecha and (periodo_FFinal>=@Fecha or periodo_FFinal is null)"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("Fecha", fecha))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idCategoria", categoria.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idBonificacion", bonificacion.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroDouble("ValorBonificacion", valorBonificacion))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)


            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un nivel de bonificación para los datos aportados")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                nivelBonificacion = dts.Tables(0).Rows(0)("Nombre")
            End If

            tr.Confirmar()

            Return nivelBonificacion

        End Using

    End Function

End Class
