<Serializable()> _
Public Class TipoContenidoDocDN
    Inherits Framework.DatosNegocio.EntidadBaseDN

    Public Sub New()

    End Sub

    Public Sub New(ByVal pId As String, ByVal pNombre As String, ByVal pGuid As String)
        mID = pId
        mNombre = pNombre
        mGUID = pGuid
    End Sub
End Class
<Serializable()> _
Public Class ColTipoContenidoDocDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of TipoContenidoDocDN)


End Class