Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos


Public Class LocalizacionesAD

    Public Function RecuperarLocalidadesPorCodigoPostal(ByVal pCodigoPostal As String) As FN.Localizaciones.DN.ColLocalidadDN

        Using tr As New Transaccion

            Dim parametros As List(Of System.Data.IDataParameter)
            Dim sql As String

            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(ParametrosConstAD.ConstParametroString("CodigoPostal", pCodigoPostal))

            sql = "SELECT IDLocalidad FROM vwLocalidadxCodigoPostal WHERE CodigoPostal=@CodigoPostal"

            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)

            Dim ds As DataSet = ej.EjecutarDataSet(sql, parametros)

            Dim colLocalidades As New FN.Localizaciones.DN.ColLocalidadDN()

            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            For Each fila As DataRow In ds.Tables(0).Rows
                Dim loc As FN.Localizaciones.DN.LocalidadDN = gi.Recuperar(fila("IDLocalidad").ToString(), GetType(FN.Localizaciones.DN.LocalidadDN))
                If Not loc Is Nothing Then
                    colLocalidades.Add(loc)
                End If
            Next

            tr.Confirmar()

            Return colLocalidades

        End Using

    End Function
End Class

