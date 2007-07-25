Imports Framework.DatosNegocio

<Serializable()> _
Public Class ComisionRVSVDN
    Inherits EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


#Region "Atributos"

    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColComisionRV As ColComisionRVDN
    Protected mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
    Protected mComision As ComisionDN
    Protected mOperadoraplicable As String
    Protected mValorCacheado As ComisionRVDN ' este valor no debe guardarse en base de datos

#End Region

#Region "Constructores"

    Public Sub New()
        CambiarValorRef(Of ColComisionRVDN)(New ColComisionRVDN, mColComisionRV)
    End Sub

#End Region

#Region "Propiedades"

    Public Property Operadoraplicable() As String
        Get
            Return mOperadoraplicable
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mOperadoraplicable)
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
     Public Property Cobertura() As FN.Seguros.Polizas.DN.CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.CoberturaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColImpuestoRV")> _
    Public Property ColComisionRV() As ColComisionRVDN
        Get
            Return mColComisionRV
        End Get
        Set(ByVal value As ColComisionRVDN)
            CambiarValorRef(Of ColComisionRVDN)(value, mColComisionRV)
        End Set
    End Property

#End Region

#Region "Propiedades ISuministradorValorDN"

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            mIRecSumiValorLN = value
        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return mValorCacheado
        End Get
    End Property

#End Region

#Region "Métodos ISuministradorValorDN"

    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor
        Dim miTarif As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()

        If miTarif Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("Tarifa no puede ser nulo para PrimabaseRVSVDN")
        End If

        If miTarif.RecuperarCobertura(Me.mCobertura.GUID) Is Nothing Then
            ' dado que los productos no continen esta cobertura este modulador no debe contar y devuelve 0 como valor neutro de coeficiente
            Return 0
        End If

        Dim colComisiones As New ColComisionRVDN
        colComisiones.AddRangeObject(mColComisionRV.Recuperar(miTarif.FEfecto))
        Dim miComision As ComisionRVDN

        Select Case colComisiones.Count
            Case Is = 0
                Throw New ApplicationException("Se debia haber recuperado al menos una comisión")
            Case Is = 1
                miComision = colComisiones.Item(0)
            Case Else
                Throw New ApplicationException("Se debia haber recuperado SOLO una comisión activa")
        End Select

        If miComision.Cobertura.GUID <> Me.mCobertura.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la cobertura de la primabase y del recuperador de valor debieran ser iguales")
        End If

        mValorCacheado = miComision

        Return miComision.Valor
    End Function

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

#End Region

#Region "Métodos"

    Private Function RecupearTarifa() As FN.Seguros.Polizas.DN.TarifaDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Return o
            End If
        Next

        Return Nothing

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mComision Is Nothing Then
            pMensaje = "El objeto mComision no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCobertura Is Nothing Then
            pMensaje = "El objeto mCobertura no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        For Each comisionRV As ComisionRVDN In Me.mColComisionRV
            If comisionRV.Comision.GUID <> Me.mComision.GUID OrElse comisionRV.Cobertura.GUID <> Me.mCobertura.GUID Then
                pMensaje = "alguno de las ComisionesRVDN dispone de una cobertura distinta a la del ComisionRVSVDN"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region


    Public Sub Limpiar() Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.Limpiar
        mIRecSumiValorLN = Nothing
        '  mColComisionRV As ColComisionRVDN
        '    mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
        '   mComision As ComisionDN
        '  mOperadoraplicable As String
        '    mValorCacheado As ComisionRVDN ' este valor no debe guardarse en base de datos

    End Sub
End Class

<Serializable()> _
Public Class ColComisionRVSVDN
    Inherits ArrayListValidable(Of ComisionRVSVDN)

    Public Function RecuperarxCobertura(ByVal nombreComision As String, ByVal nombreCobertura As String) As ColComisionRVSVDN
        Dim colCRVSVDN As New ColComisionRVSVDN
        For Each comisionRVSV As ComisionRVSVDN In Me
            If comisionRVSV.Cobertura.Nombre = nombreCobertura AndAlso comisionRVSV.Comision.Nombre = nombreComision Then
                colCRVSVDN.Add(comisionRVSV)
            End If
        Next

        Return colCRVSVDN

    End Function

End Class