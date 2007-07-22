Imports Framework.DatosNegocio

<Serializable()> _
Public Class ImpuestoRVSVDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColImpuestoRV As ColImpuestoRVDN
    Protected mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
    Protected mImpuesto As ImpuestoDN
    Protected mOperadoraplicable As String
    Protected mValorCacheado As ImpuestoRVDN ' este valor no debe guardarse en base de datos







    Public Sub New()
        CambiarValorRef(Of ColImpuestoRVDN)(New ColImpuestoRVDN, mColImpuestoRV)

    End Sub

    Public Property Operadoraplicable() As String

        Get
            Return mOperadoraplicable
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mOperadoraplicable)

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
     Public Property Cobertura() As FN.Seguros.Polizas.DN.CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.CoberturaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property


    <RelacionPropCampoAtribute("mColImpuestoRV")> _
    Public Property ColImpuestoRV() As ColImpuestoRVDN
        Get
            Return mColImpuestoRV
        End Get
        Set(ByVal value As ColImpuestoRVDN)
            CambiarValorRef(Of ColImpuestoRVDN)(value, mColImpuestoRV)
        End Set
    End Property


    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor



        Dim mitarif As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()
        If mitarif Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("tarifa no puede ser nothing para PrimabaseRVSVDN")
        End If

        If mitarif.RecuperarCobertura(Me.mCobertura.GUID) Is Nothing Then
            ' dado que los productos no continen esta cobertura este modulador no debe contar y devuelve 0 como valor neutro de coeficiente
            Return 0
        End If



        Dim colimpuestos As New ColImpuestoRVDN
        colimpuestos.AddRangeObject(ColImpuestoRV.Recuperar(mitarif.FEfecto))
        Dim miimpuesto As ImpuestoRVDN


        Select Case colimpuestos.Count
            Case Is = 0
                Throw New ApplicationException("Se debia haber recuperado al menos")

            Case Is = 1
                miimpuesto = colimpuestos.Item(0)

            Case Else
                Throw New ApplicationException("Se debia haber recuperado SOLO un impuesto activo")

        End Select


        If miimpuesto.Cobertura.GUID <> Me.mCobertura.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la cobertura de la primabase y del recuperador de valor debieran ser iguales")
        End If

        mValorCacheado = miimpuesto
        Return miimpuesto.Valor

    End Function



    Private Function RecupearTarifa() As FN.Seguros.Polizas.DN.TarifaDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Return o
            End If

        Next
        Return Nothing
    End Function

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            mIRecSumiValorLN = value
        End Set
    End Property




    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mImpuesto Is Nothing Then
            pMensaje = "mImpuesto no puede ser nothing"
            Return EstadoIntegridadDN.Inconsistente
        End If


        If mCobertura Is Nothing Then
            pMensaje = "mCobertura no puede ser nothing"
            Return EstadoIntegridadDN.Inconsistente
        End If


        For Each imp As ImpuestoRVDN In Me.mColImpuestoRV
            If imp.Impuesto.GUID <> Me.mImpuesto.GUID OrElse imp.Cobertura.GUID <> Me.mCobertura.GUID Then
                pMensaje = "alguno de los ImpuestoRVDN dispone de una cobertura distienta a la del ImpuestoRVSVDN"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return mValorCacheado
        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

End Class






<Serializable()> _
Public Class ColImpuestoRVSVDN
    Inherits ArrayListValidable(Of ImpuestoRVSVDN)

End Class




