Imports Framework.DatosNegocio

<Serializable()> _
Public Class PresupuestoDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"

    Protected mTarifa As TarifaDN
    Protected mEmisora As EmisoraPolizasDN
    Protected mFechaAnulacion As Date
    Protected mCondicionesPago As FN.GestionPagos.DN.CondicionesPagoDN

    Protected mFechaAltaSolicitada As Date
    Protected mFuturoTomador As FuturoTomadorDN

    ' Protected mFuturoTomador As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    'Protected mPeriodoValidez As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN
    'Protected mCIFNIFTomador As String
    'Protected mNombreTomador As String
    'Protected mApellido1Tomador As String
    'Protected mApellido2Tomador As String
    Protected mCodColaborador As String


    Protected mAlcanzaEstadoPoliza As Boolean

#End Region


#Region "Constructores"

    Public Sub New()
        CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(New FN.GestionPagos.DN.CondicionesPagoDN, mCondicionesPago)
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN)(New Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN, mPeriodo)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"





    Public ReadOnly Property AlcanzaEstadoPolizaX() As Boolean
        Get
            Return Me.AlcanzaestadoPoliza(Nothing)
        End Get
    End Property




    Public Property CodColaborador() As String

        Get
            Return mCodColaborador
        End Get

        Set(ByVal value As String)
            If value = "0" Then
                value = Nothing
            End If
            CambiarValorVal(Of String)(value, mCodColaborador)

        End Set
    End Property


    Public Property FechaAltaSolicitada() As Date
        Get
            Return mFechaAltaSolicitada
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaAltaSolicitada)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCondicionesPago")> _
    Public Property CondicionesPago() As FN.GestionPagos.DN.CondicionesPagoDN
        Get
            Return mCondicionesPago
        End Get
        Set(ByVal value As FN.GestionPagos.DN.CondicionesPagoDN)
            CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(value, mCondicionesPago)
        End Set
    End Property

    Public Property FechaAnulacion() As Date
        Get
            Return mFechaAnulacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaAnulacion)
        End Set
    End Property

    ''' <summary>
    ''' Devuelve el atributo mPeriodo de mi entidad base
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <RelacionPropCampoAtribute("mPerido")> _
    Public Property PeridoValidez() As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN
        Get
            Return mPeriodo
        End Get
        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN)(value, mPeriodo)
        End Set
    End Property

    <RelacionPropCampoAtribute("mEmisora")> _
    Public Property Emisora() As EmisoraPolizasDN
        Get
            Return mEmisora
        End Get
        Set(ByVal value As EmisoraPolizasDN)
            CambiarValorRef(Of EmisoraPolizasDN)(value, mEmisora)
        End Set
    End Property

    <RelacionPropCampoAtribute("mTarifa")> _
    Public Property Tarifa() As TarifaDN
        Get
            Return mTarifa
        End Get
        Set(ByVal value As TarifaDN)
            CambiarValorRef(Of TarifaDN)(value, mTarifa)
        End Set
    End Property

    Public Property FuturoTomador() As FuturoTomadorDN
        Get
            Return mFuturoTomador
        End Get
        Set(ByVal value As FuturoTomadorDN)
            CambiarValorRef(Of FuturoTomadorDN)(value, mFuturoTomador)
        End Set
    End Property

    '<RelacionPropCampoAtribute("mFuturoTomador")> _
    'Public Property FuturoTomador() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    '    Get
    '        Return mFuturoTomador
    '    End Get
    '    Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
    '        CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mFuturoTomador)
    '    End Set
    'End Property

    'Public Property CIFNIFTomador() As String
    '    Get
    '        Return mCIFNIFTomador
    '    End Get
    '    Set(ByVal value As String)
    '        CambiarValorVal(Of String)(value, mCIFNIFTomador)
    '    End Set
    'End Property

    'Public Property NombreTomador() As String
    '    Get
    '        Return mNombreTomador
    '    End Get
    '    Set(ByVal value As String)
    '        CambiarValorVal(Of String)(value, mNombreTomador)
    '    End Set
    'End Property

    'Public Property Apellido1Tomador() As String
    '    Get
    '        Return mApellido1Tomador
    '    End Get
    '    Set(ByVal value As String)
    '        CambiarValorVal(Of String)(value, mApellido1Tomador)
    '    End Set
    'End Property

    'Public Property Apellido2Tomador() As String
    '    Get
    '        Return mApellido2Tomador
    '    End Get
    '    Set(ByVal value As String)
    '        CambiarValorVal(Of String)(value, mApellido2Tomador)
    '    End Set
    'End Property

#End Region

#Region "Métodos"

    'Public Function ClonarPresupuesto() As PresupuestoDN
    '    Dim presupuestoClon As PresupuestoDN

    '    presupuestoClon = Me.CloneSuperficialSinIdentidad()
    '    presupuestoClon.PeridoValidez = Nothing
    '    If Me.mTarifa IsNot Nothing Then
    '        presupuestoClon.mTarifa = Me.mTarifa.CloneTarifa()
    '    End If
    '    presupuestoClon.mFechaAnulacion = Date.MinValue

    '    Return presupuestoClon
    'End Function






    Public Function AlcanzaestadoPoliza(ByRef pMensaje As String) As Boolean


        mAlcanzaEstadoPoliza = True

        If mCondicionesPago Is Nothing Then
            pMensaje = "No se han vinculado condiciones de pago al presuopuesto"
            mAlcanzaEstadoPoliza = False
        End If

        mCondicionesPago.NumeroRecibos = Me.Tarifa.Fraccionamiento.NumeroPagos

        If mCondicionesPago.NumeroRecibos < 1 Then
            pMensaje = "Al menos debe establecerse la posibilidad de emitir un recibo"
            mAlcanzaEstadoPoliza = False
        Else

            If Not mCondicionesPago.EstadoIntegridad(pMensaje) = EstadoIntegridadDN.Consistente Then
                mAlcanzaEstadoPoliza = False
            End If
        End If


        ' debo tener el cif del tomador
        If Me.FuturoTomador Is Nothing Then
            pMensaje = "FuturoTomador no puede ser nothing"
            mAlcanzaEstadoPoliza = False


        Else
            If String.IsNullOrEmpty(Me.FuturoTomador.NIFCIFFuturoTomador) Then
                pMensaje = "NIFCIFFuturoTomador no puede ser nulo"
                mAlcanzaEstadoPoliza = False
            Else
                If Not FuturoTomador.EstadoIntegridad(pMensaje) = EstadoIntegridadDN.Consistente Then
                    mAlcanzaEstadoPoliza = False
                End If
            End If

        End If




        If Me.mFechaAltaSolicitada = Date.MinValue Then
            pMensaje = "se debe establecer una fecha de alta"
            mAlcanzaEstadoPoliza = False
        End If


        If Me.mEmisora Is Nothing Then
            pMensaje = "la emisora no puede ser nothing"
            mAlcanzaEstadoPoliza = False
        End If




        ' almenos debo de tener algun producto ofertado en la tarida

        If Me.mPeriodo Is Nothing Then
            pMensaje = "la mPeriodo no puede ser nothing"
            mAlcanzaEstadoPoliza = False
        End If


        If Me.mTarifa Is Nothing Then
            pMensaje = "la mTarifa no puede ser nothing"
            mAlcanzaEstadoPoliza = False
        End If


        If Not Me.mTarifa.Riesgo.RiesgoValidoPoliza Then
            pMensaje = "la Riesgo no puede ser nothing"
            mAlcanzaEstadoPoliza = False
        End If


        Return mAlcanzaEstadoPoliza

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        'If mFuturoTomador Is Nothing Then
        '    pMensaje = "La entidad del futuro tomador no puede ser nula"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If
        AlcanzaestadoPoliza(Nothing)

        If mTarifa Is Nothing Then
            pMensaje = "Debe existir una entidad tarifa del presupuesto con un cuestionario resuelto"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not Me.mPeriodo.Contiene(Me.mTarifa.FEfecto) Then
            pMensaje = "La fecha de efecto de la tarifa debe estar contenida en el perido de validez del presupuesto"
            Return EstadoIntegridadDN.Inconsistente

        End If

        If mEmisora Is Nothing Then
            pMensaje = "La entidad emisora de la poliza  no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function



    Public Overrides Function ToString() As String
        Return Me.mPeriodo.ToString & " " & Me.mFuturoTomador.ToString
    End Function

#End Region

End Class

