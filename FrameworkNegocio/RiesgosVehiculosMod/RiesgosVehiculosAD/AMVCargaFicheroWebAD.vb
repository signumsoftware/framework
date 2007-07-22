Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos

''' <summary>
''' Esta clase no debería estar aquí, si utiliza 
''' </summary>
''' <remarks></remarks>
Public Class AMVCargaFicheroWebAD

    Public Function RecuperarListadoFicheroWeb() As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim dts As DataSet

        ' construir la sql y los parametros

        Using tr As New Transaccion()

            sql = "Select * from TempTarifa"

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql)

            tr.Confirmar()

            Return dts

        End Using

    End Function

    ''' <summary>
    ''' Método que devuelve el contenido de texto de un fichero. Debería estar en Ficheros
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ObtenerContenidoFicheroWeb(ByVal rutaCompletaF As String) As String

        Using tr As New Transaccion()
            Dim texto As String
            Dim sr As New System.IO.StreamReader(rutaCompletaF)

            texto = sr.ReadToEnd()
            sr.Close()

            tr.Confirmar()

            Return texto

        End Using

    End Function

End Class
