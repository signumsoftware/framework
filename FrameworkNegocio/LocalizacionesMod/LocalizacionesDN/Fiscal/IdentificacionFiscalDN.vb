''' <summary>
''' esta interface debe ser implementada por aquellas entidades den que puedas trabajar a modo de entidad juridica
''' como es el caso de la persona o la empresa
''' </summary>
''' <remarks>ReadOnly Property Correcta() As Boolean especifica que la entidad dispone de un identificador fiscal y que este esta correctamente formateado para su tipo </remarks>
Public Interface IEntidadFiscalDN
    Inherits Framework.DatosNegocio.IEntidadTemporalDN

    Property IdentificacionFiscal() As IIdentificacionFiscal
    ReadOnly Property DenominacionFiscal() As String
    Property DomicilioFiscal() As DireccionNoUnicaDN
    Property NombreComercial() As String
    ReadOnly Property Correcta() As Boolean

    Property EntidadFiscalGenerica() As EntidadFiscalGenericaDN

End Interface


<Serializable()> Public Class ColIEntidadFiscalDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of IEntidadFiscalDN)
End Class


Public Interface IIdentificacionFiscal
    Property Codigo() As String
    Function ValCodigo(ByVal pCodigo As String, ByRef pMensaje As String) As Boolean
End Interface
