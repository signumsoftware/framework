<Serializable()> Public Class TipoViaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Public Sub New()

    End Sub
    Public Sub New(ByVal pNombre As String)
        Me.mNombre = pNombre
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub
End Class
