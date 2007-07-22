
<Serializable()> Public Class HuellaContenedorRTFDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of ContenedorRTFDN)

    Public Sub New()

    End Sub

    Public Sub New(ByVal pContenedorRTF As ContenedorRTFDN)
        MyBase.New(pContenedorRTF, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacion)
    End Sub

    Public Overrides Function ToString() As String
        Return "[RTF]"
    End Function

End Class