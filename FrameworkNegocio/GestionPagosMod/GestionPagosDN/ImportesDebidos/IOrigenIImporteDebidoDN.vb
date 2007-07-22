
Imports Framework.DatosNegocio

''' <summary>
''' Clase que relaciona un importe debido con otras entidades que le dan origen
''' </summary>
''' <remarks></remarks>
Public Interface IOrigenIImporteDebidoDN
    Inherits Framework.DatosNegocio.IEntidadDN
    Property IImporteDebidoDN() As IImporteDebidoDN
    Property FAnulacion() As Date
    Property ColHEDN() As Framework.DatosNegocio.ColHEDN
    Function Anulable(ByRef pMensaje As String) As Boolean
    Function Anular(ByVal fAnulacion As Date) As Object

End Interface




<Serializable()> _
Public Class ColIOrigenIImporteDebidoDN
    Inherits ArrayListValidable(Of IOrigenIImporteDebidoDN)

End Class




