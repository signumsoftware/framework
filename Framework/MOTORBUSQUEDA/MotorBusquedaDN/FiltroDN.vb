<Serializable()> _
Public Class FiltroDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mNombreFiltro As String
    Protected mNombreVistaSel As String
    Protected mNombreVistaVis As String
    Protected mConsultaSQL As String
    Protected mcondiciones As New ColICondicionDN
    Protected mEstructura As EstructuraVistaDN
    Protected mColOperacionesPosibles As New Framework.Procesos.ProcesosDN.ColOperacionDN
    Protected mPropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN
    Protected mNombreTipoReferido As String

    Public Property NombreFiltro() As String
        Get
            Return Me.mNombreFiltro
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreFiltro)
        End Set
    End Property

    Public Property NombreVistaSel() As String
        Get
            Return Me.mNombreVistaSel
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreVistaSel)
        End Set
    End Property

    Public Property NombreVistaVis() As String
        Get
            Return Me.mNombreVistaVis
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreVistaVis)
        End Set
    End Property

    Public Property ConsultaSQL() As String
        Get
            Return Me.mConsultaSQL
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mConsultaSQL)
        End Set
    End Property

    Public Property Condiciones() As ColICondicionDN
        Get
            Return Me.mcondiciones
        End Get
        Set(ByVal value As ColICondicionDN)
            Me.CambiarValorCol(Of ColICondicionDN)(value, Me.mcondiciones)
        End Set
    End Property

    Public Property Estructura() As EstructuraVistaDN
        Get
            Return Me.mEstructura
        End Get
        Set(ByVal value As EstructuraVistaDN)
            Me.CambiarValorRef(Of EstructuraVistaDN)(value, Me.mEstructura)
        End Set
    End Property

    Public Property ColOperacionesPosibles() As Framework.Procesos.ProcesosDN.ColOperacionDN
        Get
            Return Me.mColOperacionesPosibles
        End Get
        Set(ByVal value As Framework.Procesos.ProcesosDN.ColOperacionDN)
            Me.CambiarValorCol(Of Framework.Procesos.ProcesosDN.ColOperacionDN)(value, Me.mColOperacionesPosibles)
        End Set
    End Property

    Public Property PropiedadDeInstancia() As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN
        Get
            Return Me.mPropiedadDeInstancia
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN)
            Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN)(value, Me.mPropiedadDeInstancia)
        End Set
    End Property

    Public Property TipoReferido() As System.Type
        Get
            Dim tipo As System.Type
            Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(mNombreTipoReferido, Nothing, tipo)
            Return tipo

        End Get
        Set(ByVal value As System.Type)
            Me.CambiarValorVal(Of String)(Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.TipoToString(value), mNombreTipoReferido)
        End Set
    End Property

End Class
