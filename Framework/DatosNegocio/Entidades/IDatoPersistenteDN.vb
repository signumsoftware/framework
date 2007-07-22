''' <summary>Esta interface permite definir objetos con control de estado de consistencia y modificacion.</summary>
Public Interface IDatoPersistenteDN
    'Inherits IValorSinOrden
#Region "Propiedades"
    Property ID() As String
    Property GUID() As String


    ''' <summary>Obtiene o modifica el estado de modificacion del objeto.</summary>
    Property EstadoDatos() As EstadoDatosDN

    ''' <summary>Obtiene o modifica la fecha de modificacion del objeto.</summary>
    Property FechaModificacion() As DateTime

    ''' <summary>Obtiene o modifica si el objeto esta dado de baja o no.</summary>
    Property Baja() As Boolean
    ReadOnly Property HashValores() As String
#End Region

#Region "Metodos"
    ''' <summary>Obtiene informacion sobre el estado de consistencia del objeto.</summary>
    ''' <param name="pMensaje" type="String">
    ''' Mensaje informativo sobre el estado del objeto.
    ''' </param>
    ''' <returns>El estado de consistencia del objeto</returns>
    Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN

    ''' <summary>Metodo que registra a todas las partes de una entidad.</summary>
    Sub RegistrarParteTodas()
    Sub RegistrarParte(ByVal Parte As Object)
    Sub DesRegistrarParte(ByVal Parte As Object)
    Function ActualizarHashValores() As String


#End Region

End Interface


'Public Interface IValorSinOrden
'    ''' <summary>
'    ''' valor que debe ser unico para una combinacion de valores de la clase
'    ''' es un valor cacheado y para poder conocer con seguridad el valor actual primero debe llamarse a el metodo ActualizarHashValores
'    ''' en el caso de las  clases unicos debiera de llamarse siempre a este metodo  anted de ser enviado al servidorya que en ela base de datos
'    ''' este campo sera marcado como unico
'    ''' hambavos valores deben ser sobre escibibles
'    ''' </summary>
'    ''' <value></value>
'    ''' <returns></returns>
'    ''' <remarks></remarks>
'    ReadOnly Property HashValores() As String

'    Function ActualizarHashValores() As String


'End Interface