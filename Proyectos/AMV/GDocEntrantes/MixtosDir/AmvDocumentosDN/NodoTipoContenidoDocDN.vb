
'Public Class NodoTipoContenidoDocDN
'    Inherits Framework.DatosNegocio.Arboles.NodoBaseDN
'#Region "Atributos"
'    Protected mColTipoContenidoDoc As ColTipoContenidoDocDN
'#End Region




'End Class
<Serializable()> _
Public Class NodoTipoContenidoDocDN
    Inherits Framework.DatosNegocio.Arboles.NodoBaseTDN(Of TipoContenidoDocDN)
End Class
<Serializable()> _
Public Class colNodoTipoContenidoDocDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of NodoTipoContenidoDocDN)
End Class