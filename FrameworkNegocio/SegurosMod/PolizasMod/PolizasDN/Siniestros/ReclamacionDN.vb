Imports Framework.DatosNegocio


<Serializable()> _
Public Class ReclamacionDN
    Inherits EntidadTemporalDN


#Region "Atributos"

    'fi fecha inicio
    'ff fecha de resolucion o cierre
    Protected mSiniestro As SiniestroDN
    Protected mTipoReclamacion As TipoReclamacionDN
    Protected mEstadoTramitacion As EstadoTramitacionDN
    Protected mColTramitadores As FN.Empresas.DN.ColEmpleadosDN
    Protected mContraria As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mColTramitadoresContrarios As Framework.DatosNegocio.ColIEntidadDN
    Protected mCobertura As CoberturaDN
    Protected mCodigoReclContraria As String
    Protected mTipoDaño As TipoDañoDN
    Protected mFUltimaRevision As Date
    Protected mFComunicacion As Date
    Protected mFPrescripcion As Date



#End Region

#Region "Constructores"

#End Region

#Region "Propiedades"










    Public Property FPrescripcion() As Date

        Get
            Return mFPrescripcion
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFPrescripcion)

        End Set
    End Property






    ''' <summary>
    ''' cuando se comunicó la exitencia de la reclamacion a AMV
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property FComunicacion() As Date

        Get
            Return mFComunicacion
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFComunicacion)

        End Set
    End Property






    Public Property FUltimaRevision() As Date

        Get
            Return mFUltimaRevision
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFUltimaRevision)

        End Set
    End Property












    <RelacionPropCampoAtribute("mTipoDaño")> _
    Public Property TipoDaño() As TipoDañoDN
        Get
            Return mTipoDaño
        End Get
        Set(ByVal value As TipoDañoDN)
            CambiarValorRef(Of TipoDañoDN)(value, mTipoDaño)
        End Set
    End Property









    Public Property CodigoReclContraria() As String

        Get
            Return mCodigoReclContraria
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCodigoReclContraria)

        End Set
    End Property









    <RelacionPropCampoAtribute("mCobertura")> _
    Public Property Cobertura() As CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As CoberturaDN)
            CambiarValorRef(Of CoberturaDN)(value, mCobertura)
        End Set
    End Property








    <RelacionPropCampoAtribute("mColTramitadoresContrarios")> _
    Public Property ColTramitadoresContrarios() As Framework.DatosNegocio.ColIEntidadDN
        Get
            Return mColTramitadoresContrarios
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColIEntidadDN)
            CambiarValorRef(Of Framework.DatosNegocio.ColIEntidadDN)(value, mColTramitadoresContrarios)
        End Set
    End Property






    <RelacionPropCampoAtribute("mContraria")> _
    Public Property Contraria() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
        Get
            Return mContraria
        End Get
        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mContraria)
        End Set
    End Property








    <RelacionPropCampoAtribute("mColTramitadores")> _
    Public Property ColTramitadores() As FN.Empresas.DN.ColEmpleadosDN
        Get
            Return mColTramitadores
        End Get
        Set(ByVal value As FN.Empresas.DN.ColEmpleadosDN)
            CambiarValorRef(Of FN.Empresas.DN.ColEmpleadosDN)(value, mColTramitadores)
        End Set
    End Property






    <RelacionPropCampoAtribute("mSiniestro")> _
    Public Property Siniestro() As SiniestroDN
        Get
            Return mSiniestro
        End Get
        Set(ByVal value As SiniestroDN)
            CambiarValorRef(Of SiniestroDN)(value, mSiniestro)
        End Set
    End Property







    <RelacionPropCampoAtribute("mTipoReclamacion")> _
    Public Property TipoReclamacion() As TipoReclamacionDN
        Get
            Return mTipoReclamacion
        End Get
        Set(ByVal value As TipoReclamacionDN)
            CambiarValorRef(Of TipoReclamacionDN)(value, mTipoReclamacion)
        End Set
    End Property






    <RelacionPropCampoAtribute("mEstadoTramitacion")> _
    Public Property EstadoTramitacion() As EstadoTramitacionDN
        Get
            Return mEstadoTramitacion
        End Get
        Set(ByVal value As EstadoTramitacionDN)
            CambiarValorRef(Of EstadoTramitacionDN)(value, mEstadoTramitacion)
        End Set
    End Property







#End Region

#Region "Metodos"

    Public Sub AltaDesdeSiniestro(ByVal pSiniestro As SiniestroDN)


        Me.Siniestro = pSiniestro
        Me.FI = Me.mSiniestro.FI




    End Sub




#End Region



End Class




<Serializable()> _
Public Class ColReclamacionDN
    Inherits ArrayListValidable(Of ReclamacionDN)
End Class







