''' <summary>
''' Esta interfaz proporciona la funcionalidad minima de todos los elementos de logica de negocio que no utilicen el
''' motor de persistencia de acceso a datos.
''' </summary>
Public Interface IBaseLN

#Region "Metodos"
    ''' <summary>Guarda un objeto.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que vamos a guardar.
    ''' </param>
    ''' <returns>El objeto que hemos guardado.</returns>
    Function Guardar(ByVal pObjeto As Object) As Object

    ''' <summary>Recupera un objeto a partir de un identificador.</summary>
    ''' <param name="pId" type="String">
    ''' Id del objeto a recuperar.
    ''' </param>
    ''' <returns>El objeto que queremos recuperar.</returns>
    Function Recuperar(ByVal pId As String) As Object

    ''' <summary>Elimina un objeto.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que vamos a eliminar.
    ''' </param>
    ''' <returns>El objeto eliminado.</returns>
    Function Eliminar(ByVal pObjeto As Object) As Object
#End Region

End Interface
