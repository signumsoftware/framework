Public Class TipoAgrupacionMapDN
    Inherits Framework.DatosNegocio.TipoConOrdenDN
    Public Overrides Function ToXML() As String
        Return Me.ToXML(GetType(TipoAgrupacionMapXML))
    End Function
    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        Return FromXML(GetType(TipoAgrupacionMapXML), ptr)
    End Function
End Class


Public Enum TiposAgrupacionMap
    Elemento
    Solapas
    Filas
    Columnas
End Enum

Public Class TipoAgrupacionMapXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    <Xml.Serialization.XmlAttribute()> Public id As String
    <Xml.Serialization.XmlAttribute()> Public Nombre As String
 
    Public Overridable Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As TipoAgrupacionMapDN
        entidad = pEntidad

        id = entidad.ID
        Nombre = entidad.Nombre
    End Sub

    Public Overridable Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As TipoAgrupacionMapDN
        entidad = pEntidad

        entidad.ID = id
        entidad.Nombre = Me.Nombre
    End Sub
End Class