Imports Framework.DatosNegocio

Imports FN.GestionPagos.DN

<Serializable()> _
Public Class FraccionamientoRVDN
    Inherits EntidadTemporalDN

#Region "Atributos"

    Protected mValor As Double
    Protected mCobertura As Seguros.Polizas.DN.CoberturaDN
    Protected mFraccionamiento As FraccionamientoDN
    Protected mFraccionable As Boolean
    Protected mOperadorAplicable As String

#End Region

#Region "Propiedades"

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

    <RelacionPropCampoAtribute("mFraccionamiento")> _
       Public Property Fraccionamiento() As FraccionamientoDN
        Get
            Return mFraccionamiento
        End Get
        Set(ByVal value As FraccionamientoDN)
            CambiarValorRef(Of FraccionamientoDN)(value, mFraccionamiento)
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

    Public Property Fraccionable() As Boolean
        Get
            Return mFraccionable
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mFraccionable)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String

        cadena = Valor.ToString()

        If mCobertura IsNot Nothing Then
            cadena = cadena & " - " & mCobertura.ToString()
        End If

        If mFraccionamiento IsNot Nothing Then
            cadena = cadena & " - " & mFraccionamiento.ToString()
        End If

        Return cadena

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mFraccionamiento Is Nothing Then
            pMensaje = "mFraccionamiento no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCobertura Is Nothing Then
            pMensaje = "mCobertura no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function
#End Region

End Class


<Serializable()> _
Public Class ColFraccionamientoRVDN
    Inherits ColEntidadTemporalBaseDN(Of FraccionamientoRVDN)

#Region "Métodos"

    Public Function VerificarIntegridadColCompleta() As ColFraccionamientoRVDN
        ' no pueden haber dos fraccionamientos activos solapados  para el mismo fraccionamiento y cobertura
        Dim colFraccionamiento As ColFraccionamientoDN = Me.RecuperarFraccionamientos()
        Dim colCobertura As FN.Seguros.Polizas.DN.ColCoberturaDN = Me.RecuperarCoberturas()

        For Each cob As FN.Seguros.Polizas.DN.CoberturaDN In colCobertura
            For Each frac As FraccionamientoDN In colFraccionamiento

                Dim colFracRV As ColFraccionamientoRVDN = Me.SeleccionarX(cob, frac)
                Dim ColIntervaloFechas As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN = colFracRV.RecuperarColPeridosFechas()

                Dim par As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos
                par = ColIntervaloFechas.PrimeroNoCumple(IntSolapadosOContenido.Libres)

                If Not par Is Nothing Then
                    Return colFracRV.RecuperarXPar(par)
                End If
            Next
        Next

        Return New ColFraccionamientoRVDN()
    End Function

    Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ColFraccionamientoRVDN
        Dim col As New ColFraccionamientoRVDN

        For Each impRV As FraccionamientoRVDN In Me

            If impRV.Periodo Is pPar.Int1 OrElse impRV.Periodo Is pPar.Int2 Then
                col.Add(impRV)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarColPeridosFechas() As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
        Dim col As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN

        For Each fracRV As FraccionamientoRVDN In Me
            col.Add(fracRV.Periodo)
        Next

        Return col
    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN) As ColFraccionamientoRVDN
        Dim col As New ColFraccionamientoRVDN

        For Each fracRV As FraccionamientoRVDN In Me
            If fracRV.Cobertura.GUID = pCoberturta.GUID Then
                col.Add(fracRV)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarFraccionamientos() As ColFraccionamientoDN
        Dim col As New ColFraccionamientoDN

        For Each fracRV As FraccionamientoRVDN In Me
            col.AddUnico(fracRV.Fraccionamiento)
        Next

        Return col
    End Function

    Public Function RecuperarCoberturas() As FN.Seguros.Polizas.DN.ColCoberturaDN
        Dim col As New FN.Seguros.Polizas.DN.ColCoberturaDN

        For Each fracRV As FraccionamientoRVDN In Me
            col.AddUnico(fracRV.Cobertura)
        Next

        Return col
    End Function

    Public Function RecuperarxCobFrac(ByVal cobertura As FN.Seguros.Polizas.DN.CoberturaDN, ByVal nombreFraccionamiento As String) As ColFraccionamientoRVDN
        Dim col As New ColFraccionamientoRVDN

        For Each fracRV As FraccionamientoRVDN In Me
            If fracRV.Fraccionamiento.Nombre = nombreFraccionamiento AndAlso fracRV.Cobertura.GUID = cobertura.GUID Then
                col.Add(fracRV)
            End If
        Next

        Return col
    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN, ByVal fraccionamiento As FraccionamientoDN) As ColFraccionamientoRVDN
        Dim col As New ColFraccionamientoRVDN

        For Each fracRV As FraccionamientoRVDN In Me
            If fracRV.Fraccionamiento.GUID = fraccionamiento.GUID AndAlso fracRV.Cobertura.GUID = pCoberturta.GUID Then
                col.Add(fracRV)
            End If
        Next

        Return col
    End Function

#End Region

End Class