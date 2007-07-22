Imports Framework.DatosNegocio
<Serializable()> _
Public Class LiquidacionMapDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN


    Protected mAplazamiento As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
    Protected mTipoCalculoImporte As TipoCalculoImporte
    ' Protected mColHEnetRelacioandas As Framework.DatosNegocio.ColHEDN
    Protected mPorcentageOValor As Double
    Protected mEntidadLiquidadora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mHeCausaLiquidacion As Framework.DatosNegocio.HEDN
    Protected mCausaPrimaModulada As Boolean


    ''' <summary>
    ''' contine el codigo de un gruppo de liquidacion
    ''' 
    ''' sedá cuando esa liquidacion se realiza a una o varias entidades fiscales y la seleccion de estas requeire más datos que los que expone el mapeado
    ''' 
    ''' debiera exitir un liqueidador concreto que supiera liquidar este tipo de liquidaciones 
    ''' </summary>
    ''' <remarks></remarks>
    Protected mCodGrupoLiquidacion As String

    Public Sub New()
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mAplazamiento)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub








    Public Property CausaPrimaModulada() As Boolean

        Get
            Return mCausaPrimaModulada
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mCausaPrimaModulada)

        End Set
    End Property







    Public Property CodGrupoLiquidacion() As String

        Get
            Return mCodGrupoLiquidacion
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCodGrupoLiquidacion)

        End Set
    End Property






    <RelacionPropCampoAtribute("mHeCausaLiquidacion")> _
    Public Property HeCausaLiquidacion() As Framework.DatosNegocio.HEDN

        Get
            Return mHeCausaLiquidacion
        End Get

        Set(ByVal value As Framework.DatosNegocio.HEDN)
            CambiarValorRef(Of Framework.DatosNegocio.HEDN)(value, mHeCausaLiquidacion)

        End Set
    End Property







    <RelacionPropCampoAtribute("mEntidadLiquidadora")> _
    Public Property EntidadLiquidadora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN

        Get
            Return mEntidadLiquidadora
        End Get

        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mEntidadLiquidadora)

        End Set
    End Property






    Public Property PorcentageOValor() As Double

        Get
            Return mPorcentageOValor
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mPorcentageOValor)

        End Set
    End Property










    <RelacionPropCampoAtribute("mAplazamiento")> _
    Public Property Aplazamiento() As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias

        Get
            Return mAplazamiento
        End Get

        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(value, mAplazamiento)

        End Set
    End Property






    Public Property TipoCalculoImporte() As TipoCalculoImporte

        Get
            Return mTipoCalculoImporte
        End Get

        Set(ByVal value As TipoCalculoImporte)
            CambiarValorVal(Of TipoCalculoImporte)(value, mTipoCalculoImporte)

        End Set
    End Property



    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        If mAplazamiento Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el aplazamiento no puede ser nulo")
        End If

        If Me.mEntidadLiquidadora Is Nothing AndAlso String.IsNullOrEmpty(Me.mCodGrupoLiquidacion) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el gurpo de liquidacion y la entidad liquidadora no pueden ser nulos a la vez")
        End If


        If mCausaPrimaModulada Then

            If Not Me.mHeCausaLiquidacion Is Nothing Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN(" si  se aplica a  la prima modulada, no se puede elejir una causa de liquidacion")
            End If

        Else


            If Me.mHeCausaLiquidacion Is Nothing Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("es necesaria una causa de liquidacion, si no se aplica a toda la prima modulada")
            End If
        End If



        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




    '<RelacionPropCampoAtribute("mColHEnetRelacioandas")> _
    'Public Property ColHEnetRelacioandas() As Framework.DatosNegocio.ColHEDN

    '    Get
    '        Return mColHEnetRelacioandas
    '    End Get

    '    Set(ByVal value As Framework.DatosNegocio.ColHEDN)
    '        CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(value, mColHEnetRelacioandas)

    '    End Set
    'End Property







End Class




Public Enum TipoCalculoImporte
    Porcentual
    Fijo
End Enum






<Serializable()> _
Public Class ColLiquidacionMapDN
    Inherits ArrayListValidable(Of LiquidacionMapDN)

    ''' <summary>
    ''' recupera las entidades del mapedo que refieren a alguna de las entidades referidas en la colde huellas
    ''' </summary>
    ''' <param name="pcolhedn"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function FiltrarXEntidadesReferidas(ByVal pcolhedn As Framework.DatosNegocio.ColHEDN) As ColLiquidacionMapDN


        'Dim colliq As New ColLiquidacionMapDN


        'For Each liq As LiquidacionMapDN In Me
        '    For Each he As Framework.DatosNegocio.HEDN In pcolhedn
        '        If liq.ColHEnetRelacioandas.Contiene(he, CoincidenciaBusquedaEntidadDN.Todos) Then
        '            colliq.Add(liq)
        '        End If
        '    Next
        'Next


        'Return colliq




        Dim colliq As New ColLiquidacionMapDN


        For Each liq As LiquidacionMapDN In Me
            For Each he As Framework.DatosNegocio.HEDN In pcolhedn
                If Not liq.HeCausaLiquidacion Is Nothing AndAlso liq.HeCausaLiquidacion.GUIDReferida = he.GUIDReferida Then
                    colliq.AddUnico(liq)
                End If
            Next
        Next


        Return colliq

    End Function



End Class




