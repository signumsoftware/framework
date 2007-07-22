Imports Framework.DatosNegocio

<Serializable()> _
Public Class ComisionRVDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    Protected mValor As Double
    Protected mCobertura As Seguros.Polizas.DN.CoberturaDN
    Protected mComision As ComisionDN
    Protected mOperadorAplicable As String

    Public Property OperadorAplicable() As String
        Get
            Return mOperadorAplicable
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mOperadorAplicable)
        End Set
    End Property

    Public Property Valor() As Double
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, Me.mValor)
        End Set
    End Property

    <RelacionPropCampoAtribute("mComision")> _
       Public Property Comision() As ComisionDN
        Get
            Return mComision
        End Get
        Set(ByVal value As ComisionDN)
            CambiarValorRef(Of ComisionDN)(value, mComision)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCobertura")> _
    Public Property Cobertura() As Seguros.Polizas.DN.CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As Seguros.Polizas.DN.CoberturaDN)
            Me.CambiarValorRef(Of Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mComision Is Nothing Then
            pMensaje = "El objeto ComisionDN no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCobertura Is Nothing Then
            pMensaje = "El objeto CoberturaDN no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String

        cadena = mValor.ToString()

        If mCobertura IsNot Nothing Then
            cadena = cadena & " - " & mCobertura.ToString()
        End If

        If mComision IsNot Nothing Then
            cadena = cadena & " - " & mComision.ToString()
        End If

        Return cadena

    End Function

#End Region


End Class

<Serializable()> _
Public Class ColComisionRVDN
    Inherits Framework.DatosNegocio.ColEntidadTemporalBaseDN(Of ComisionRVDN)

    Public Function VerificarIntegridadColCompleta() As ColComisionRVDN
        ' no pueden haber dos comisiones activas solapadas para la misma comisión y cobertura

        Dim colComisiones As ColComisionDN = Me.RecuperarComisiones
        Dim colCobertura As FN.Seguros.Polizas.DN.ColCoberturaDN = Me.RecuperarCoberturas

        For Each cob As FN.Seguros.Polizas.DN.CoberturaDN In colCobertura
            For Each comision As ComisionDN In colComisiones

                Dim ColComisionRVDN As ColComisionRVDN = Me.SeleccionarX(cob, comision)
                Dim ColIntervaloFechas As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN = ColComisionRVDN.RecuperarColPeridosFechas

                Dim par As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos
                par = ColIntervaloFechas.PrimeroNoCumple(IntSolapadosOContenido.Libres)
                If Not par Is Nothing Then
                    Return ColComisionRVDN.RecuperarXPar(par)
                End If

            Next
        Next

        Return New ColComisionRVDN()

    End Function

    Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ColComisionRVDN
        Dim colC As New ColComisionRVDN

        For Each comisionRV As ComisionRVDN In Me
            If comisionRV.Periodo Is pPar.Int1 OrElse comisionRV.Periodo Is pPar.Int2 Then
                colC.Add(comisionRV)
            End If
        Next

        Return colC
    End Function

    Public Function RecuperarColPeridosFechas() As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
        Dim colC As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN

        For Each comisionRV As ComisionRVDN In Me
            colC.Add(comisionRV.Periodo)
        Next

        Return colC
    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN) As ColComisionRVDN
        Dim colC As New ColComisionRVDN

        For Each comisionRV As ComisionRVDN In Me
            If comisionRV.Cobertura.GUID = pCoberturta.GUID Then
                colC.Add(comisionRV)
            End If
        Next

        Return colC

    End Function

    Public Function RecuperarComisiones() As ColComisionDN
        Dim colC As New ColComisionDN

        For Each comisionRV As ComisionRVDN In Me
            colC.AddUnico(comisionRV.Comision)
        Next

        Return colC

    End Function

    Public Function RecuperarCoberturas() As FN.Seguros.Polizas.DN.ColCoberturaDN
        Dim col As New FN.Seguros.Polizas.DN.ColCoberturaDN

        For Each comisionRV As ComisionRVDN In Me
            col.AddUnico(comisionRV.Cobertura)
        Next

        Return col

    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pComision As FN.RiesgosVehiculos.DN.ComisionDN) As ColComisionRVDN
        Dim colC As New ColComisionRVDN

        For Each comisionRV As ComisionRVDN In Me
            If comisionRV.Comision.GUID = pComision.GUID AndAlso comisionRV.Cobertura.GUID = pCoberturta.GUID Then
                colC.Add(comisionRV)
            End If
        Next

        Return colC

    End Function

End Class