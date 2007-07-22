Imports Framework.DatosNegocio

<Serializable()> _
Public Class ImpuestoRVDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    Protected mValor As Double
    Protected mCobertura As Seguros.Polizas.DN.CoberturaDN

    Protected mImpuesto As ImpuestoDN
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


    <RelacionPropCampoAtribute("mImpuesto")> _
       Public Property Impuesto() As ImpuestoDN
        Get
            Return mImpuesto
        End Get
        Set(ByVal value As ImpuestoDN)
            CambiarValorRef(Of ImpuestoDN)(value, mImpuesto)
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

    Public Overrides Function ToString() As String
        Dim cadena As String

        cadena = Valor.ToString()

        If mCobertura IsNot Nothing Then
            cadena = cadena & " - " & mCobertura.ToString()
        End If

        If mImpuesto IsNot Nothing Then
            cadena = cadena & " - " & mImpuesto.ToString()
        End If

        Return cadena

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mImpuesto Is Nothing Then
            pMensaje = "mImpuesto no puede ser nothing"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCobertura Is Nothing Then
            pMensaje = "mCobertura no puede ser nothing"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function
#End Region



End Class




<Serializable()> _
Public Class ColImpuestoRVDN
    Inherits Framework.DatosNegocio.ColEntidadTemporalBaseDN(Of ImpuestoRVDN)


    Public Function VerificarIntegridadColColpleta() As ColImpuestoRVDN
        ' no pueden haber dos impuestos activos solapados  para el mismo impuesto y cobertura


        Dim colImpuestos As ColImpuestoDN = Me.RecuperarImpuestos
        Dim colCobertura As FN.Seguros.Polizas.DN.ColCoberturaDN = Me.RecuperarCoberturas


        For Each cob As FN.Seguros.Polizas.DN.CoberturaDN In colCobertura
            For Each imp As ImpuestoDN In colImpuestos

                Dim ColImpuestoRVDN As ColImpuestoRVDN = Me.SeleccionarX(cob, imp)
                Dim ColIntervaloFechas As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN = ColImpuestoRVDN.RecuperarColPeridosFechas

                Dim par As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos
                par = ColIntervaloFechas.PrimeroNoCumple(IntSolapadosOContenido.Libres)
                If Not par Is Nothing Then
                    Return ColImpuestoRVDN.RecuperarXPar(par)
                End If

            Next
        Next

        Return New ColImpuestoRVDN





    End Function


    Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ColImpuestoRVDN
        Dim col As New ColImpuestoRVDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            If ImpuestoRV.Periodo Is pPar.Int1 OrElse ImpuestoRV.Periodo Is pPar.Int2 Then
                col.Add(ImpuestoRV)
            End If
        Next

        Return col
    End Function


    Public Function RecuperarColPeridosFechas() As Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN
        Dim col As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            col.Add(ImpuestoRV.Periodo)

        Next

        Return col
    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN) As ColImpuestoRVDN

        Dim col As New ColImpuestoRVDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            If ImpuestoRV.Cobertura.GUID = pCoberturta.GUID Then
                col.Add(ImpuestoRV)
            End If

        Next

        Return col


    End Function


    Public Function RecuperarImpuestos() As ColImpuestoDN


        Dim col As New ColImpuestoDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            col.AddUnico(ImpuestoRV.Impuesto)

        Next

        Return col

    End Function
    Public Function RecuperarCoberturas() As FN.Seguros.Polizas.DN.ColCoberturaDN


        Dim col As New FN.Seguros.Polizas.DN.ColCoberturaDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            col.AddUnico(ImpuestoRV.Cobertura)

        Next

        Return col

    End Function

    Public Function SeleccionarX(ByVal pCoberturta As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pImpuesto As FN.RiesgosVehiculos.DN.ImpuestoDN) As ColImpuestoRVDN

        Dim col As New ColImpuestoRVDN

        For Each ImpuestoRV As ImpuestoRVDN In Me

            If ImpuestoRV.Impuesto.GUID = pImpuesto.GUID AndAlso ImpuestoRV.Cobertura.GUID = pCoberturta.GUID Then
                col.Add(ImpuestoRV)
            End If

        Next

        Return col


    End Function
End Class




