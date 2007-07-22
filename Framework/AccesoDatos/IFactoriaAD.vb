''' <summary>
''' Esta interfaz permite implementar factorias que proporcionen conectores y adaptadores dependiendo de un recurso.
''' </summary>
Public Interface IFactoriaAD

#Region "Metodos"
    ''' <summary>Este metodo proporciona una conexion para un recurso.</summary>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso para el que queremos la conexion.
    ''' </param>
    ''' <returns>La conexion adecuada para nuestro recurso.</returns>
    Function GetConexion(ByVal pRecurso As IRecursoLN) As IDbConnection

    ''' <summary>Este metodo proporciona un adaptador para un recurso y un comando.</summary>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso para el que queremos el adaptador.
    ''' </param>
    ''' <param name="pComando" type="IDBCommand">
    ''' Comando para el que queremos el adaptador.
    ''' </param>
    ''' <returns>El adaptador adecuado para nuestro recurso segun nuestro comando.</returns>
    Function GetAdaptador(ByVal pRecurso As IRecursoLN, ByVal pComando As IDbCommand) As IDbDataAdapter
#End Region

End Interface
