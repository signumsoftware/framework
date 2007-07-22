<Serializable()> Public Class TipoSexo
    Inherits Framework.DatosNegocio.EntidadDN

    Public Overrides Function ToString() As String
        Return MyBase.Nombre
    End Function
End Class
