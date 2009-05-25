<Serializable()> _
Public Class ComandoOperacionDN
    Inherits Framework.DatosNegocio.EntidadDN
    Public IDRelacion As String
    Public Tipo As System.Type
    Public tipoOperacion As AmvDocumentosDN.TipoOperacionREnF = TipoOperacionREnF.FijarEstado
    Public EstadoSolicitado As EstadosRelacionENFichero
    Public Resultado As Boolean
    Public Mensaje As String

End Class
<Serializable()> _
Public Class ColComandoOperacionDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ComandoOperacionDN)
End Class