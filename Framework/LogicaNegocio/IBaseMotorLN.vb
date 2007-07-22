''' <summary>
''' Esta interfaz proporciona la funcionalidad minima de todos los elementos de logica de negocio que utilicen
''' el motor de persistencia de acceso a datos.
''' </summary>
Public Interface IBaseMotorLN

#Region "Metodos"
    ''' <summary>Guarda un objeto.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que vamos a guardar.
    ''' </param>
    ''' <returns>El resultado de la operacion de guardado.</returns>
    Function Guardar(ByVal pObjeto As Object) As OperacionGuardarLN

    ''' <summary>Recupera un objeto a partir de un identificador y su tipo.</summary>
    ''' <param name="pId" type="String">
    ''' Id del objeto a recuperar.
    ''' </param>
    ''' <param name="pTipo" type="Type">
    ''' Tipo del objeto a recuperar.
    ''' </param>
    ''' <returns>El objeto que queremos recuperar.</returns>
    Function Recuperar(ByVal pId As String, ByVal pTipo As Type) As Object

    ''' <summary>Recupera una coleccion a partir de una coleccion de identificadores y su tipo.</summary>
    ''' <param name="pColID" type="IList">
    ''' Coleccion de ids a recuperar.
    ''' </param>
    ''' <param name="pTipo" type="Type">
    ''' Tipo de los objetos a recuperar.
    ''' </param>
    ''' <returns>La coleccion de objetos que queremos recuperar.</returns>
    Function Recuperar(ByVal pColID As IList, ByVal pTipo As Type) As IList

    ''' <summary>Da de baja un objeto.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que vamos a dar de baja.
    ''' </param>
    ''' <returns>El objeto dado de baja.</returns>
    Function Baja(ByVal pObjeto As Object) As Object
#End Region

End Interface
