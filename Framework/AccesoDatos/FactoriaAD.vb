''' <summary>
''' Esta clase representa una factoria que proporciona conexiones y adaptadores adecuadas para un determinado recurso.
''' </summary>
Public Class FactoriaAD
    Implements IFactoriaAD

#Region "Metodos"
    ''' <summary>Este metodo proporciona una conexion para un recurso.</summary>
    ''' <remarks>Lanza una NotSupportedException si el recurso no esta soportado.</remarks>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso para el que queremos la conexion.
    ''' </param>
    ''' <returns>La conexion adecuada para nuestro recurso.</returns>
    Public Overridable Function GetConexion(ByVal pRecurso As IRecursoLN) As IDbConnection Implements IFactoriaAD.GetConexion
        Select Case pRecurso.Tipo.ToLower
            Case "oledb"
                Return New OleDb.OleDbConnection(pRecurso.Dato("connectionstring"))

            Case "sqls"
                Return New SqlClient.SqlConnection(pRecurso.Dato("connectionstring"))
        End Select

        Throw New NotSupportedException
    End Function

    ''' <summary>Este metodo proporciona un adaptador para un recurso y un comando.</summary>
    ''' <remarks>Lanza una NotSupportedException si el recurso no esta soportado.</remarks>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso para el que queremos el adaptador.
    ''' </param>
    ''' <param name="pComando" type="IDBCommand">
    ''' Comando para el que queremos el adaptador.
    ''' </param>
    ''' <returns>El adaptador adecuado para nuestro recurso segun nuestro comando.</returns>
    Public Overridable Function GetAdaptador(ByVal pRecurso As IRecursoLN, ByVal pComando As IDbCommand) As IDbDataAdapter Implements IFactoriaAD.GetAdaptador
        Select Case pRecurso.Tipo.ToLower
            Case "oledb"
                Return New OleDb.OleDbDataAdapter(pComando)

            Case "sqls"
                Return New SqlClient.SqlDataAdapter(pComando)
        End Select

        Throw New NotSupportedException
    End Function
#End Region

End Class
