Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports FN.GestionPagos.DN
Imports FN.Trabajos.DN
'Imports FN.Localizaciones.DN

<Serializable()> _
Public Class PeriodoCoberturaDN
    Inherits EntidadTemporalDN

#Region "Campos"

    Protected mColTarifasPrevias As ColTarifaDN

    Protected mTarifa As TarifaDN
    'Protected mIntervaloFechas As IntervaloFechasDN
    Protected mCondicionesPago As FN.GestionPagos.DN.CondicionesPagoDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()

        CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(New FN.GestionPagos.DN.CondicionesPagoDN, mCondicionesPago)
        Me.CambiarValorCol(Of ColTarifaDN)(New ColTarifaDN(), mColTarifasPrevias)
        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

#End Region

#Region "Propiedades"











    <RelacionPropCampoAtribute("mCondicionesPago")> _
    Public Property CondicionesPago() As FN.GestionPagos.DN.CondicionesPagoDN

        Get
            Return mCondicionesPago
        End Get

        Set(ByVal value As FN.GestionPagos.DN.CondicionesPagoDN)
            CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(value, mCondicionesPago)
            ' establecer el numero de pagos
            Me.mCondicionesPago.NumeroRecibos = mTarifa.Fraccionamiento.NumeroPagos

        End Set
    End Property

    <RelacionPropCampoAtribute("mColTarifasPrevias")> _
    Public Property ColTarifasPrevias() As ColTarifaDN

        Get
            Return mColTarifasPrevias
        End Get

        Set(ByVal value As ColTarifaDN)
            CambiarValorRef(Of ColTarifaDN)(value, mColTarifasPrevias)

        End Set
    End Property






    <RelacionPropCampoAtribute("mTarifa")> _
    Public Property Tarifa() As TarifaDN
        Get
            Return mTarifa
        End Get
        Set(ByVal value As TarifaDN)
            AsignarNuevaTarifa(value)
        End Set
    End Property



#End Region

#Region "Métodos de validación"

#End Region









    Public Function BajaAFecha(ByVal pfecha As Date, ByVal pMensaje As String) As Boolean


        If Me.Periodo.FF <> Date.MinValue Then
            pMensaje = "error: la entidad ya esta anulada a fecha:" & Me.Periodo.FF
            Return False
        End If

        If pfecha = Date.MinValue OrElse pfecha < Me.Periodo.FI Then
            pMensaje = "error: pfecha = Date.MinValue OrElse pfecha < Me.Periodo.FI"
            Return False
        End If



        Me.FF = pfecha
        Return True



    End Function

    Public Sub AsignarNuevaTarifa(ByVal pTarifaDN As TarifaDN)

        If Not mTarifa Is Nothing AndAlso mTarifa.GUID = pTarifaDN.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("se trata de la misma tarifa referida en tarifa")
        End If
        If Me.mColTarifasPrevias.Contiene(pTarifaDN, CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la tarifa suministrada ya forma parte de las tarifas previas")
        End If

        ' verificar que las coberturas no cambian
        If Not mTarifa Is Nothing AndAlso Not mTarifa.CoberturasIguales(pTarifaDN) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("Todas las tarifas referidas deben tener las mismas coberturas declaradas, para formar parte del mismo perido de cobertura")
        End If

        If Not mTarifa Is Nothing AndAlso Not Me.mColTarifasPrevias.Contiene(mTarifa, CoincidenciaBusquedaEntidadDN.Todos) Then
            Me.mColTarifasPrevias.Add(Me.mTarifa)
        End If

        'Me.Tarifa = pTarifaDN
        CambiarValorRef(Of TarifaDN)(pTarifaDN, mTarifa)

        ' establecer el numero de pagos
        Me.mCondicionesPago.NumeroRecibos = mTarifa.Fraccionamiento.NumeroPagos

    End Sub



    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Me.mTarifa Is Nothing Then
            pMensaje = "Un perido de cobertura debe de referir a una tarifa"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Me.mTarifa.Riesgo Is Nothing OrElse Not mTarifa.Riesgo.EstadoConsistentePoliza Then
            pMensaje = "Un perido de cobertura requiere que la tarifa refiera a un riesgo consistente"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Me.mCondicionesPago Is Nothing Then
            pMensaje = "debe existir unas condiciones de pago de la poliza"
            Return EstadoIntegridadDN.Inconsistente
        End If


        ' establecer el numero de pagos
        Me.mCondicionesPago.NumeroRecibos = mTarifa.Fraccionamiento.NumeroPagos

        If Me.mCondicionesPago.NumeroRecibos < 1 Then
            pMensaje = "debe exitir al menos la posibilidad de emitir un pago"
            Return EstadoIntegridadDN.Inconsistente
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Me.mToSt = Me.mPeriodo.ToString & "  COBs: " & Me.mTarifa.RecuperarCoberturas.ToString()
        Return mToSt
    End Function

End Class


<Serializable()> _
Public Class ColPeriodoCoberturaDN
    Inherits ArrayListValidable(Of PeriodoCoberturaDN)

#Region "Métodos"

    Public Function RecuperarActivos() As ColPeriodoCoberturaDN
        Dim colIF As New ColPeriodoCoberturaDN()

        For Each periodoC As PeriodoCoberturaDN In Me
            If periodoC.FF = Date.MinValue Then
                colIF.Add(periodoC)
            End If
        Next

        Return colIF
    End Function

    Public Function PeriodosCoberturaSolapados() As Boolean
        If RecuperarMiColIntervaloFechas.IntervalosFechaSolapados Then
            Return True
        End If

        Return False
    End Function

    Public Function RecuperarMiColIntervaloFechas() As ColIntervaloFechasDN
        Dim colIF As New ColIntervaloFechasDN()

        For Each periodoC As PeriodoCoberturaDN In Me
            colIF.Add(periodoC.Periodo)
        Next

        Return colIF
    End Function

    Public Function RecuperarxFechaInicio(ByVal pFI As Date) As PeriodoCoberturaDN

        For Each periodoC As PeriodoCoberturaDN In Me
            If periodoC.FI = pFI Then
                Return periodoC
            End If
        Next

        Return Nothing
    End Function

    Public Function RecuperarxFechaFinal(ByVal pFf As Date) As PeriodoCoberturaDN
        For Each periodoC As PeriodoCoberturaDN In Me
            If periodoC.FF = pFf Then
                Return periodoC
            End If
        Next

        Return Nothing
    End Function

    Public Function RecuperarPrimeroContengaFecha(ByVal pFechaContenida As Date) As PeriodoCoberturaDN
        For Each periodoC As PeriodoCoberturaDN In Me
            If periodoC.Periodo.Contiene(pFechaContenida) Then
                Return periodoC
            End If
        Next

        Return Nothing
    End Function

#End Region


End Class