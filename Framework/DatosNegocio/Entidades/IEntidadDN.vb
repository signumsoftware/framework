Public Interface IModificable

    ''' <summary>Obtiene la fecha de modificacion de la entidad.</summary>
    ReadOnly Property FechaModificacion() As DateTime

    ''' <summary>Evento que indica que hubo un cambio en los datos del objeto.</summary>
    Event CambioEstadoDatos(ByVal sender As Object)
End Interface



''' <summary>Esta interfaz proporciona los datos minimos de una entidad de datos con persistencia.</summary>
Public Interface IEntidadDN
    Inherits IEntidadBaseDN, IModificable


#Region "Propiedades"
    '  ''' <summary>Obtiene el estado de modificacion de la entidad.</summary>
    '  ReadOnly Property Estado() As EstadoDatosDN

    '  ''' <summary>Obtiene la fecha de modificacion de la entidad.</summary>
    '  ReadOnly Property FechaModificacion() As DateTime

    ' ''' <summary>Obtiene si la entidad esta dada de baja o no.</summary>
    ReadOnly Property Baja() As Boolean

    Property CampoUsuario(ByVal clave As String) As ICampoUsuario

    Property ColCampoUsuario() As ColCampoUsuario
    Function AsignarEntidad(ByVal pEntidad As IEntidadBaseDN) As Boolean
    Function InstanciarEntidad(ByVal pTipo As System.Type) As IEntidadBaseDN
    Function InstanciarEntidad(ByVal pTipo As System.Type, ByVal pPropidadDestino As Reflection.PropertyInfo) As IEntidadBaseDN


#End Region

#Region "Eventos"
    ' ''' <summary>Evento que indica que hubo un cambio en los datos del objeto.</summary>
    ' Event CambioEstadoDatos(ByVal sender As Object)
#End Region

End Interface

<Serializable()> Public Class ColIEntidadDN
    Inherits ArrayListValidable(Of IEntidadDN)
End Class

<Serializable()> Public Class ColIEntidadBaseDN
    Inherits ArrayListValidable(Of IEntidadBaseDN)
End Class

Public Interface ICampoUsuario
    Inherits IModificable

    Property Clave() As String
    Property Valor() As String
End Interface