Imports Framework.LogicaNegocios.Transacciones
Imports Framework.GestorSalida.DN

Public Class ConfiguracionImpresionAD

    Public Function RecuperarContenedorDescriptorImpresoraPorNombre(ByVal nombreImpresora As String) As List(Of ContenedorDescriptorImpresoraDN)
        Dim sql As String = "SELECT ID FROM tlContenedorDescriptorImpresoraDN WHERE NombreImpresora=@NombreImpresora"

        Dim params As New List(Of IDataParameter)
        params.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@NombreImpresora", nombreImpresora))

        Dim impresoras As New List(Of ContenedorDescriptorImpresoraDN)()

        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql, params)
            If Not ds Is Nothing AndAlso ds.Tables(0).Rows.Count <> 0 Then
                For a As Integer = 0 To ds.Tables(0).Rows.Count - 1
                    Dim gi As New AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    impresoras.Add(gi.Recuperar(ds.Tables(0).Rows(a)(0), GetType(ContenedorDescriptorImpresoraDN)))
                Next
            End If
            tr.Confirmar()
        End Using

        Return impresoras
    End Function

End Class
