Imports Framework.LogicaNegocios.Transacciones
Imports Framework.GestorSalida.DN

Public Class DocumentoSalidaAD

    ''' <summary>
    ''' Devuelve los 5 primeros documentos de salida que haya para un canal de salida determinado
    ''' </summary>
    Public Function RecuperarPrimerosDSPorCanal(ByVal pTipoCanal As Framework.GestorSalida.DN.CanalSalida, ByVal numeroElementos As Integer) As List(Of Framework.GestorSalida.DN.DocumentoSalida)
        Dim nombrevista As String = String.Empty
        Dim lista As New List(Of Framework.GestorSalida.DN.DocumentoSalida)()

        Select Case pTipoCanal
            Case DN.CanalSalida.impresora
                nombrevista = "vwDocumentosSalidaImpresionEnCola"
            Case DN.CanalSalida.email
                nombrevista = "vwDocumentosSalidaMailEnCola"
            Case DN.CanalSalida.fax
                nombrevista = "vwDocumentosSalidaFaxEnCola"
            Case Else
                Throw New NotImplementedException("No existe la estructura de datos en la Base de Datos para el tipo de canal solicitado (" & pTipoCanal.ToString() & ")")
        End Select

        Dim sql As String = String.Concat("SELECT TOP(", numeroElementos.ToString(), ") ID FROM ", nombrevista)

        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql)
            If Not ds Is Nothing Then
                Dim listaIDs As New ArrayList()
                For Each dr As DataRow In ds.Tables(0).Rows
                    listaIDs.Add(dr(0))
                Next
                Dim gi As New AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                lista = gi.Recuperar(listaIDs, GetType(Framework.GestorSalida.DN.DocumentoSalida), Nothing)
            End If
        End Using

        Return lista
    End Function

    Public Function RecuperarDocumentoSalidaPorTicket(ByVal ticket As String) As DocumentoSalida
        Dim respuesta As DocumentoSalida = Nothing

        Dim sql As String = "SELECT ID FROM tlDocumentoSalida WHERE Ticket=@ticket"
        Dim params As New List(Of IDataParameter)
        params.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@ticket", ticket))

        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql, params)
            If Not ds Is Nothing AndAlso ds.Tables(0).Rows.Count <> 0 Then
                Dim gi As New AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                respuesta = CType(gi.Recuperar(ds.Tables(0).Rows(0)(0), GetType(DocumentoSalida), Nothing), DocumentoSalida)
            End If
        End Using

        Return respuesta
    End Function


    Public Function RecuperarEstadoEnvioPorTicket(ByVal ticket As String) As EstadoEnvio
        Dim respuesta As EstadoEnvio = EstadoEnvio.Desconocido

        Dim sql As String = "SELECT EstadoEnvio FROM tlDocumentoSalida WHERE Ticket=@ticket"
        Dim params As New List(Of IDataParameter)
        params.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@ticket", ticket))

        Using tr As New Transaccion()
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql, params)
            If Not ds Is Nothing AndAlso ds.Tables(0).Rows.Count <> 0 Then
                respuesta = CInt(ds.Tables(0).Rows(0)(0))
            End If
        End Using

        Return respuesta
    End Function

End Class
