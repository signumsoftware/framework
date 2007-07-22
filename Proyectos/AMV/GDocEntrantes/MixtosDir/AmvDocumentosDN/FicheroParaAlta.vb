<Serializable()> _
Public Class FicheroParaAlta
    'Public HuellaFichero As FN.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN
    Public HuellaFichero As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN

    Public TipoEntidad As AmvDocumentosDN.TipoEntNegoioDN
    Public HuellaNodoTipoEntNegoio As AmvDocumentosDN.HuellaNodoTipoEntNegoioDN
    Public clanal As AmvDocumentosDN.CanalEntradaDocsDN
End Class


<Serializable()> _
Public Class DatosFicheroIncidentado
    Inherits Framework.DatosNegocio.EntidadDN
    Public Comentario As String
    Public Fecha As DateTime
End Class

<Serializable()> _
Public Class DatosDocumentoAIR
    Inherits Framework.DatosNegocio.EntidadDN
    Public Comentario As String
    Public Fecha As DateTime
    Public Operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
End Class