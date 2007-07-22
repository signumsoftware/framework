
Public Class DocumentoRespaldoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mTipoDocumentoRespaldo As TipoDocumentoRespaldoDN
    Protected mPropiedadEmpresa As PropiedadDN
    Protected mOrganoDireccionEmpresa As OrganoDireccionDN
End Class
Public Class ColDocumentoRespaldoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of DocumentoRespaldoDN)
End Class