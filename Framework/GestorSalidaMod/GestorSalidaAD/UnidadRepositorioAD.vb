Imports Framework.LogicaNegocios.Transacciones

Public Class UnidadRepositorioAD

    ''' <summary>
    ''' Devuelve las dos siguientes UnidadRepositorio temporales que estén disponibles o con capacidad de carga media
    ''' </summary>
    Public Function GetNextUnidadRepositorioTemporalDisponibles() As List(Of Framework.GestorSalida.DN.UnidadRepositorio)
        Dim lista As New List(Of Framework.GestorSalida.DN.UnidadRepositorio)

        Dim sql As String = "SELECT TOP(2) ID FROM vwRepositoriosTemporalesDisponibles"
        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql)
            If Not ds Is Nothing Then
                Dim listaIDs As New ArrayList()
                For Each dr As DataRow In ds.Tables(0).Rows
                    listaIDs.Add(dr(0))
                Next
                Dim gi As New AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                For Each ur As Framework.GestorSalida.DN.UnidadRepositorio In gi.Recuperar(listaIDs, GetType(Framework.GestorSalida.DN.UnidadRepositorio), Nothing)
                    lista.Add(ur)
                Next
            End If
            tr.Confirmar()
        End Using

        Return lista
    End Function

    ''' <summary>
    ''' Devuelve las dos siguientes UnidadRepositorio temporales que estén disponibles o con capacidad de carga media
    ''' </summary>
    Public Function GetNextUnidadRepositorioPersistenteDisponibles() As List(Of Framework.GestorSalida.DN.UnidadRepositorio)
        Dim lista As New List(Of Framework.GestorSalida.DN.UnidadRepositorio)

        Dim sql As String = "SELECT TOP(2) ID FROM vwRepositoriosPersistentesDisponibles"
        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql)
            If Not ds Is Nothing Then
                Dim listaIDs As New ArrayList()
                For Each dr As DataRow In ds.Tables(0).Rows
                    listaIDs.Add(dr(0))
                Next
                Dim gi As New AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                lista = gi.Recuperar(listaIDs, GetType(Framework.GestorSalida.DN.UnidadRepositorio), Nothing)
            End If
            tr.Confirmar()
        End Using

        Return lista
    End Function

End Class
