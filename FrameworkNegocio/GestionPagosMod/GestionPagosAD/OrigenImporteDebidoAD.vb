Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.GestionPagos.DN

Public Class OrigenImporteDebidoAD


    Public Function Recuperar(ByVal pApunteImpD As FN.GestionPagos.DN.ApunteImpDDN) As FN.GestionPagos.DN.OrigenIdevBaseDN


        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)

        Using tr As New Transaccion()

            sql = "SELECT     dbo.tlOrigenIdevBaseDN.ID, dbo.tlApunteImpDDN.ID AS idAid FROM dbo.tlOrigenIdevBaseDN INNER JOIN  dbo.tlApunteImpDDN ON dbo.tlOrigenIdevBaseDN.idIImporteDebidoDNApunteImpDDN = dbo.tlApunteImpDDN.ID WHERE     (dbo.tlApunteImpDDN.ID = @tlApunteImpDDNID)"
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("tlApunteImpDDNID", pApunteImpD.ID))
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim idid As Integer = ej.EjecutarEscalar(sql, parametros)

            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Recuperar = gi.Recuperar(Of FN.GestionPagos.DN.OrigenIdevBaseDN)(idid)

            tr.Confirmar()


        End Using

    End Function


End Class
