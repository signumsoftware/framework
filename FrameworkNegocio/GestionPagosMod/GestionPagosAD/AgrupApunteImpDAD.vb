Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.GestionPagos.DN

Public Class AgrupApunteImpDAD
    Public Function RecuperarApunteImpDebidoLibres(ByVal ag As AgrupApunteImpDDN) As ColApunteImpDDN

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)



        Using tr As New Transaccion

            If ag.PermiteCompensar Then
                sql = "SELECT  id  FROM tlApunteImpDDN WHERE   (GUIDAgrupacion is NULL) and  (FAnulacion IS NULL)  AND (FEfecto <= @FEfecto) AND ((idAcreedora = @idAcreedora) and (idDeudora = @idDeudora)) or ((idAcreedora = @idDeudora) and (idDeudora = @idAcreedora))"
            Else
                sql = "SELECT  id  FROM tlApunteImpDDN WHERE   (GUIDAgrupacion is NULL) and  (FAnulacion IS NULL)  AND (FEfecto <= @FEfecto) AND ((idAcreedora = @idAcreedora) and (idDeudora = @idDeudora)) "
            End If


            ' sql = "SELECT  id  FROM tlApunteImpDDN WHERE   (GUIDAgrupacion is NULL) and  (FAnulacion IS NULL)  AND (FEfecto <= @FEfecto) AND ((idAcreedora = @idAcreedora) and (idDeudora = @idDeudora)) AND ((idAcreedora = @idDeudora) and (idDeudora = @idAcreedora))"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idAcreedora", ag.IImporteDebidoDN.Acreedora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idDeudora", ag.IImporteDebidoDN.Deudora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FEfecto", ag.IImporteDebidoDN.FEfecto))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim dts As DataSet = ej.EjecutarDataSet(sql, parametros, False)


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            Dim col As New ColApunteImpDDN
            For Each dr As DataRow In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                col.Add(gi.Recuperar(Of FN.GestionPagos.DN.ApunteImpDDN)(dr(0)))

            Next


            tr.Confirmar()


            Return col

        End Using





    End Function

    Public Function BuscarImportesDebidosLibres(ByVal param As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN) As DataSet

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)



        Using tr As New Transaccion



            sql = "select * from vwApunteImpD where id in (SELECT  id  FROM tlApunteImpDDN WHERE   (GUIDAgrupacion is NULL) and  (FAnulacion IS NULL) AND (idAcreedora = @idAcreedora) AND (idDeudora = @idDeudora) AND (FEfecto <= @FEfecto))"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idAcreedora", param.Acreedora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idDeudora", param.Deudora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FEfecto", param.FechaEfecto))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            BuscarImportesDebidosLibres = ej.EjecutarDataSet(sql, parametros, False)

            tr.Confirmar()

        End Using





    End Function


End Class
