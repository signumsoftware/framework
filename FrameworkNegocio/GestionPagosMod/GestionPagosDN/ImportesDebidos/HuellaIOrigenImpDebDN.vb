
'<Serializable()> _
'Public Class HuellaIOrigenImpDebDN
'    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of IOrigenIImporteDebidoDN)





'    Protected mFAnulacion As Date

'    Public ReadOnly Property FAnulacion() As Date

'        Get
'            Return mFAnulacion
'        End Get


'    End Property





'    Public Sub New()

'    End Sub

'    Public Sub New(ByVal pOrigen As IOrigenIImporteDebidoDN)
'        MyBase.New(pOrigen, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
'    End Sub


'    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
'        MyBase.AsignarEntidadReferida(pEntidad)
'        Dim ent As IOrigenIImporteDebidoDN = pEntidad
'        Me.mFAnulacion = ent.FAnulacion

'    End Sub


'End Class

<Serializable()> _
Public Class HuellaIOrigenImpDebDN
    Inherits Framework.DatosNegocio.HEDN


    Protected mFAnulacion As Date

    Public ReadOnly Property FAnulacion() As Date
        Get
            Return mFAnulacion
        End Get
    End Property

    Public Sub New()

    End Sub

    Public Sub New(ByVal pOrigen As IOrigenIImporteDebidoDN)
        MyBase.New(pOrigen, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
    End Sub

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        MyBase.AsignarEntidadReferida(pEntidad)
        Dim ent As IOrigenIImporteDebidoDN = pEntidad
        Me.mFAnulacion = ent.FAnulacion
    End Sub


End Class