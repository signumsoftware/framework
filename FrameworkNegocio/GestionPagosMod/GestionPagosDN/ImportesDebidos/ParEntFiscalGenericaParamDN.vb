Imports Framework.DatosNegocio
<Serializable()> _
Public Class ParEntFiscalGenericaParamDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mAcreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mDeudora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mFechaEfecto As Date


    Protected mAgrupacion As FN.GestionPagos.DN.AgrupApunteImpDDN





    Protected mPermitirEntidadPordefecto As Boolean
    Protected mPermiteCompensar As Boolean



    Public Property PermiteCompensar() As Boolean
        Get
            Return Me.mPermiteCompensar
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mPermiteCompensar)
        End Set
    End Property


    Public Property PermitirEntidadPordefecto() As Boolean

        Get
            Return mPermitirEntidadPordefecto
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mPermitirEntidadPordefecto)

        End Set
    End Property







    <RelacionPropCampoAtribute("mAgrupacion")> _
    Public Property Agrupacion() As FN.GestionPagos.DN.AgrupApunteImpDDN

        Get
            Return mAgrupacion
        End Get

        Set(ByVal value As FN.GestionPagos.DN.AgrupApunteImpDDN)
            CambiarValorRef(Of FN.GestionPagos.DN.AgrupApunteImpDDN)(value, mAgrupacion)

        End Set
    End Property





    <RelacionPropCampoAtribute("mAcreedora")> _
    Public Property Acreedora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN

        Get
            Return mAcreedora
        End Get

        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mAcreedora)

        End Set
    End Property




    <RelacionPropCampoAtribute("mDeudora")> _
     Public Property Deudora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN

        Get
            Return mDeudora
        End Get

        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mDeudora)

        End Set
    End Property








    Public Property FechaEfecto() As Date

        Get
            Return mFechaEfecto
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaEfecto)

        End Set
    End Property










    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mAcreedora Is Nothing AndAlso mDeudora Is Nothing Then
            pMensaje = "acreedora y deudora almenos una deben estar rellenos"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not mPermitirEntidadPordefecto AndAlso mAcreedora Is Nothing OrElse mDeudora Is Nothing Then
            pMensaje = "acreedora y deudora deben estar rellenos"
            Return EstadoIntegridadDN.Inconsistente
        End If



        If mAcreedora.GUID = mDeudora.GUID Then
            pMensaje = "acreedora y deudora deben ser entidades distintas"
            Return EstadoIntegridadDN.Inconsistente
        End If



        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




End Class
