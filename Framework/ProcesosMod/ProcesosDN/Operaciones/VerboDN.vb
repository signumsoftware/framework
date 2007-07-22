<Serializable()> Public Class VerboDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mRutaIcono As String
    Protected mComentario As String

#End Region

#Region "Propiedades"

    Public Property RutaIcono() As String
        Get
            Return mRutaIcono
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mRutaIcono)
        End Set
    End Property

    Public Property Comentario() As String
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mComentario)
        End Set
    End Property

#End Region

    ' Private mVinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDN

    'Public Property VinculoMetodo() As Framework.TiposYReflexion.DN.VinculoMetodoDN
    '    Get
    '        Return Me.mVinculoMetodo
    '    End Get
    '    Set(ByVal value As Framework.TiposYReflexion.DN.VinculoMetodoDN)
    '        Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(value, Me.mVinculoMetodo)

    '    End Set
    'End Property
End Class


<Serializable()> Public Class ColVerboDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of VerboDN)
End Class


Public Class VerboDNMapXml
    Implements Framework.DatosNegocio.IXMLAdaptador

    Public VinculoMetodo As New Framework.TiposYReflexion.DN.VinculoMetodoDNMapXml

    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim mientidad As VerboDN = pEntidad
        '  VinculoMetodo.ObjetoToXMLAdaptador(mientidad.VinculoMetodo)
    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto

        Dim mientidad As VerboDN = pEntidad
        Dim vm As New Framework.TiposYReflexion.DN.VinculoMetodoDN
        VinculoMetodo.XMLAdaptadorToObjeto(vm)
        ' mientidad.VinculoMetodo = vm

    End Sub
End Class