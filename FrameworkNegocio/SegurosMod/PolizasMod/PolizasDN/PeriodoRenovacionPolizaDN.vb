Imports Framework.DatosNegocio

<Serializable()> _
Public Class PeriodoRenovacionPolizaDN
    Inherits EntidadTemporalDN

#Region "Atributos"

    Protected mColPeriodosCobertura As ColPeriodoCoberturaDN
    Protected mPoliza As PolizaDN
    Protected mFechaBaja As Date
    Protected mFCreacion As Date
    Protected mPeriodoCoberturaActivo As PeriodoCoberturaDN
    Protected mValorBonificacion As Double

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColPeriodoCoberturaDN)(New ColPeriodoCoberturaDN(), mColPeriodosCobertura)
    End Sub

    Public Sub New(ByVal pPresupuesto As PresupuestoDN)


        CambiarValorCol(Of ColPeriodoCoberturaDN)(New ColPeriodoCoberturaDN(), mColPeriodosCobertura)

    End Sub

#End Region

#Region "Propiedades"

    Public Property FCreacion() As Date

        Get
            Return mFCreacion
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFCreacion)

        End Set
    End Property

    <RelacionPropCampoAtribute("mFechaBaja")> _
    Public Property FechaBaja() As Date

        Get
            Return mFechaBaja
        End Get

        Set(ByVal value As Date)
            CambiarValorRef(Of Date)(value, mFechaBaja)

        End Set
    End Property

    <RelacionPropCampoAtribute("mColPeriodosCobertura")> _
    Public Property ColPeriodosCobertura() As ColPeriodoCoberturaDN
        Get
            Return mColPeriodosCobertura
        End Get
        Set(ByVal value As ColPeriodoCoberturaDN)
            CambiarValorCol(Of ColPeriodoCoberturaDN)(value, mColPeriodosCobertura)
        End Set
    End Property

    <RelacionPropCampoAtribute("mPoliza")> _
    Public Property Poliza() As PolizaDN
        Get
            Return mPoliza
        End Get
        Set(ByVal value As PolizaDN)
            CambiarValorRef(Of PolizaDN)(value, mPoliza)
        End Set
    End Property

    Public ReadOnly Property PeridoCoberturaActivo() As PeriodoCoberturaDN
        Get
            Dim colpca As ColPeriodoCoberturaDN = Me.mColPeriodosCobertura.RecuperarActivos
            Select Case colpca.Count


                Case Is = 0
                    Return Nothing

                Case Is = 1
                    Return colpca(0)
                Case Else
                    Throw New ApplicationException("Solo puede haber un perido de cobertura activo")
            End Select
        End Get
    End Property

    Public Property ValorBonificacion() As Double
        Get
            Return mValorBonificacion
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorBonificacion)
        End Set
    End Property

#End Region


#Region "Métodos"

    ''' <summary>
    ''' crea un nuevo perido de renovacion para una polizavlsito para ser tarificado
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overridable Function Renovacion() As PeriodoRenovacionPolizaDN



        Dim nuevoPeriodoRenovacionPoliza As New PeriodoRenovacionPolizaDN
        nuevoPeriodoRenovacionPoliza.FI = Me.Periodo.FF.AddDays(1)
        nuevoPeriodoRenovacionPoliza.FF = nuevoPeriodoRenovacionPoliza.FI.AddYears(1)
        nuevoPeriodoRenovacionPoliza.Poliza = Me.Poliza


        Dim pc As New PeriodoCoberturaDN
        pc.FI = nuevoPeriodoRenovacionPoliza.FI
        nuevoPeriodoRenovacionPoliza.ColPeriodosCobertura.Add(pc)


        Dim pcActivo As PeriodoCoberturaDN = Me.ColPeriodosCobertura.RecuperarActivos(0) ' solo debe exitir uno

        Dim tarifa As New TarifaDN
        tarifa.FEfecto = pc.FI
        tarifa.Riesgo = pcActivo.Tarifa.Riesgo
        tarifa.AMD = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
        tarifa.AMD.Anyos = pcActivo.Tarifa.AMD.Anyos
        tarifa.AMD.Dias = pcActivo.Tarifa.AMD.Dias
        tarifa.AMD.Meses = pcActivo.Tarifa.AMD.Meses
        tarifa.Fraccionamiento = pcActivo.Tarifa.Fraccionamiento

        Dim edn As EntidadDN = pcActivo.Tarifa.DatosTarifa
        tarifa.DatosTarifa = edn.CloneSinIdentidad

        For Each lp As LineaProductoDN In pcActivo.Tarifa.ColLineaProducto
            tarifa.ColLineaProducto.Add(lp.CloneSuperficialSinIdentidad)
        Next
        pc.Tarifa = tarifa

        Return nuevoPeriodoRenovacionPoliza

    End Function



    Public Function BajaAFecha(ByVal pfecha As Date, ByRef pMensaje As String) As Boolean


        If Me.FechaBaja <> Date.MinValue Then
            pMensaje = "error: la entidad ya esta anulada a fecha:" & Me.FechaBaja
            Return False
        End If

        If pfecha = Date.MinValue OrElse pfecha < Me.Periodo.FI Then

            pMensaje = "error: pfecha = Date.MinValue OrElse pfecha < Me.Periodo.FI"
            Return False

        End If



        If pfecha > Me.Periodo.FF Then

            pMensaje = "error: la fecha de anulación es mayor que la fecha de renovacion"
            Return False

        End If

        Dim colpc As ColPeriodoCoberturaDN = Me.mColPeriodosCobertura.RecuperarActivos
        If Not colpc.Item(0).BajaAFecha(pfecha, pMensaje) Then
            Return False
        End If
        Me.FechaBaja = pfecha

        Return True



    End Function

    Public Function RecuperarPrebio(ByVal pPeriodoCobertura As PeriodoCoberturaDN) As PeriodoCoberturaDN
        Return mColPeriodosCobertura.RecuperarxFechaFinal(pPeriodoCobertura.FI)
    End Function

#End Region



#Region "Métodos Validación"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        ' si no esta dado de alta no puede tener fecha de baja




        If Me.mFechaBaja > Date.MinValue AndAlso (String.IsNullOrEmpty(Me.mID)) Then
            pMensaje = "un perido de renovacion que no hasido dado de alta no peude disponer de un valor para fecha de baja"
            Return EstadoIntegridadDN.Inconsistente
        End If




        If Not Me.mPeriodo.FInicio.AddYears(1) = Me.mPeriodo.FFinal Then
            pMensaje = "La aplitud del perido debe estar fijada a un año"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not mColPeriodosCobertura.RecuperarMiColIntervaloFechas.ColIFechasContenidoIntervalo(Me.mPeriodo) Then
            pMensaje = "Los periodos de cobertura deben estar contenidos en el periodo anual"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mColPeriodosCobertura IsNot Nothing AndAlso mColPeriodosCobertura.Count > 1 AndAlso mColPeriodosCobertura.PeriodosCoberturaSolapados Then
            pMensaje = "Los periodos de cobertura dentro de un periodo anual no pueden solapar"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Dim colpc As ColPeriodoCoberturaDN = Me.ColPeriodosCobertura.RecuperarActivos

        If Me.mFechaBaja = Date.MinValue Then

            If colpc.Count <> 1 Then
                pMensaje = "debe existir un periodo de cobertura activo y solo uno"
                Return EstadoIntegridadDN.Inconsistente

            End If

        Else
            If colpc.Count <> 0 Then
                pMensaje = "No debe exisitir ningún periodo de cobertua activo dado que el periodo de renovación está en baja"
                Return EstadoIntegridadDN.Inconsistente

            End If

        End If

        Me.mPeriodoCoberturaActivo = Me.PeridoCoberturaActivo

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

End Class


<Serializable()> _
Public Class ColPeriodoRenovacionPolizaDN
    Inherits ArrayListValidable(Of PeriodoRenovacionPolizaDN)

#Region "Métodos"

    Public Function RecuperarColPeriodosCobertura() As ColPeriodoCoberturaDN
        Dim colPC As New ColPeriodoCoberturaDN()

        For Each periodoAnual As PeriodoRenovacionPolizaDN In Me
            colPC.AddRange(periodoAnual.ColPeriodosCobertura)
        Next

        Return colPC

    End Function

#End Region

End Class

<Serializable()> _
Public Class HEPeriodoRenovacionPolizaDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of PeriodoRenovacionPolizaDN)
    Public Sub New()

    End Sub
    Public Sub New(ByVal entidad As PeriodoRenovacionPolizaDN)
        MyBase.New(entidad, HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
    End Sub


    Public Sub New(ByVal entidad As Framework.DatosNegocio.HEDN)
        MyBase.New(entidad)
    End Sub
End Class