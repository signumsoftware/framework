Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.GestionPagos.DN

Public Class ApunteImpDAD

    Public Function Saldo(ByVal pAcreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN, ByVal pDeudora As FN.Localizaciones.DN.EntidadFiscalGenericaDN, ByVal pFecha As Date) As Double
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)

        Using tr As New Transaccion()


            ' todo: se puede acelerar en un procedimiento almacenado


            Dim saldoAcreedor, saldodeudor As Double

            sql = "SELECT   SUM(Importe) AS Saldo   FROM tlApunteImpDDN WHERE   (GUIDAgrupacion is NULL) and  (FAnulacion IS NULL) AND (idAcreedora = @idAcreedora) AND (idDeudora = @idDeudora) AND (FEfecto <= @FEfecto)"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idAcreedora", pAcreedora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idDeudora", pDeudora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FEfecto", pFecha))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)


            Dim resultado As Object = ej.EjecutarEscalar(sql, parametros) ' lo que el deudor debe al acreedor

            If resultado Is DBNull.Value Then
                saldoAcreedor = 0
            Else
                saldoAcreedor = resultado
            End If


            sql = "SELECT   SUM(Importe) AS Saldo   FROM tlApunteImpDDN WHERE  (GUIDAgrupacion is NULL) and   (FAnulacion IS NULL) AND (idAcreedora = @idAcreedora) AND (idDeudora = @idDeudora) AND (FEfecto <= @FEfecto)"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idAcreedora", pDeudora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idDeudora", pAcreedora.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FEfecto", pFecha))
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            resultado = ej.EjecutarEscalar(sql, parametros) ' lo que el acreedor debe al deudor

            If resultado Is DBNull.Value Then
                saldodeudor = 0
            Else
                saldodeudor = resultado
            End If

            tr.Confirmar()

            Return saldoAcreedor - saldodeudor

        End Using
    End Function
End Class
