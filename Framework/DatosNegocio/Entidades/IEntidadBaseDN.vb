''' <summary>Esta interfaz proporciona los datos minimos de una entidad de datos sin persistencia.</summary>
Public Interface IEntidadBaseDN

#Region "Propiedades"
    ''' <summary>Obtiene o modifica el id de la entidad.</summary>
    Property ID() As String
    ReadOnly Property GUID() As String

    ''' <summary>Obtiene el estado de modificacion de la entidad.</summary>
    ReadOnly Property Estado() As EstadoDatosDN

    ''' <summary>Obtiene o modifica el nombre de la entidad.</summary>
    Property Nombre() As String

    Function Typo() As System.Type

    Function ClaveEntidad() As String
    Function ToXML() As String
    Function FromXML(ByVal ptr As IO.TextReader) As Object
    Function ToString() As String

    ''' <summary>
    ''' funcion encrgada de verificar si dos objetos representan la misma entidad de negocio
    ''' </summary>
    ''' <param name="pEntidad">la entidad a evaaluar</param>
    ''' <param name="pMismaRef">indica si es el mismo objeto en memoria</param>
    ''' <returns>true si representa a la msima entidad, esta función deberá ser sobre escrita incluyendo los campos clave de la entidad, si no es seguro que estos campos clave solo puedan estar asoociados a un guid</returns>
    ''' <remarks></remarks>
    Function RepresentaMismaEntidad(ByVal pEntidad As IEntidadBaseDN, ByRef mensaje As String, ByRef pMismaRef As Boolean) As Boolean
    Function ToHtGUIDs(ByVal phtGUIDEntidades As Hashtable, ByRef clones As ColIEntidadDN) As Hashtable
    ReadOnly Property IdentificacionTexto()
#End Region

End Interface
