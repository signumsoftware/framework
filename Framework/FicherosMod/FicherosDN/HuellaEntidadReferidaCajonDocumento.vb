<Serializable()> _
Public Class HuellaEntidadReferidaCajonDocumentoDN
    Inherits Framework.DatosNegocio.HEDN
End Class

<Serializable()> _
Public Class ColHuellaEntidadReferidaCajonDocumentoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of HuellaEntidadReferidaCajonDocumentoDN)

    Public Function AddHuellaPara(ByVal iEntidad As Framework.DatosNegocio.IEntidadDN) As HuellaEntidadReferidaCajonDocumentoDN
        Dim h As New HuellaEntidadReferidaCajonDocumentoDN()
        h.EntidadReferida = iEntidad
        h.EliminarEntidadReferida()
        Me.Add(h)
        Return h
    End Function
    Public Function AddUnicoHuellaPara(ByVal iEntidad As Framework.DatosNegocio.IEntidadDN) As HuellaEntidadReferidaCajonDocumentoDN
        Dim h As New HuellaEntidadReferidaCajonDocumentoDN()
        h.EntidadReferida = iEntidad
        h.EliminarEntidadReferida()
        Me.AddUnico(h)
        Return h
    End Function
End Class
