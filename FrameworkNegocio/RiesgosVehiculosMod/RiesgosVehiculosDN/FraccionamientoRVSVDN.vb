Imports Framework.DatosNegocio

Imports FN.GestionPagos.DN

<Serializable()> _
Public Class FraccionamientoRVSVDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


#Region "Atributos"

    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColFraccionamientoRV As ColFraccionamientoRVDN
    Protected mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
    'Protected mFraccionamiento As FraccionamientoDN
    Protected mOperadoraplicable As String
    Protected mValorCacheado As FraccionamientoRVDN ' este valor no debe guardarse en base de datos

#End Region

#Region "Constructores"

    Public Sub New()
        CambiarValorRef(Of ColFraccionamientoRVDN)(New ColFraccionamientoRVDN, mColFraccionamientoRV)
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

    '<RelacionPropCampoAtribute("mFraccionamiento")> _
    '   Public Property Fraccionamiento() As FraccionamientoDN
    '    Get
    '        Return mFraccionamiento
    '    End Get
    '    Set(ByVal value As FraccionamientoDN)
    '        CambiarValorRef(Of FraccionamientoDN)(value, mFraccionamiento)
    '    End Set
    'End Property

    <RelacionPropCampoAtribute("mCobertura")> _
     Public Property Cobertura() As FN.Seguros.Polizas.DN.CoberturaDN
        Get
            Return mCobertura
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.CoberturaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property


    <RelacionPropCampoAtribute("mColFraccionamientoRV")> _
    Public Property ColFraccionamientoRV() As ColFraccionamientoRVDN
        Get
            Return mColFraccionamientoRV
        End Get
        Set(ByVal value As ColFraccionamientoRVDN)
            CambiarValorRef(Of ColFraccionamientoRVDN)(value, mColFraccionamientoRV)
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
        'If mFraccionamiento Is Nothing Then
        '    pMensaje = "mFraccionamiento no puede ser nulo"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        If mCobertura Is Nothing Then
            pMensaje = "mCobertura no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        For Each frac As FraccionamientoRVDN In mColFraccionamientoRV
            'If frac.Fraccionamiento.GUID <> mFraccionamiento.GUID OrElse frac.Cobertura.GUID <> mCobertura.GUID Then
            '    pMensaje = "Alguno de los FraccionamientosRVDN dispone de una cobertura o tipo de fraccionamiento distinto a la del FraccionamientoRVSV"
            '    Return EstadoIntegridadDN.Inconsistente
            'End If
            If frac.Cobertura.GUID <> mCobertura.GUID Then
                pMensaje = "Alguno de los FraccionamientosRVDN dispone de una cobertura distinta a la del FraccionamientoRVSV"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

#Region "Métodos ISuministradorValorDN"

    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()
        Dim fracRV As FraccionamientoRVDN

        If tarifa Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("La tarifa no puede ser nula para FraccionamientoRVSVDN")
        End If

        If tarifa.RecuperarCobertura(Me.mCobertura.GUID) Is Nothing Then
            ' dado que los productos no continen esta cobertura este modulador no debe contar y devuelve 0 como valor neutro de coeficiente
            Return 0
        End If

        Dim colFracxTipoFrac As New ColFraccionamientoRVDN()
        Dim colFraccionamientos As New ColFraccionamientoRVDN()

        colFracxTipoFrac.AddRangeObject(mColFraccionamientoRV.RecuperarxCobFrac(Me.Cobertura, tarifa.NombreFraccionaminento))
        colFraccionamientos.AddRangeObject(colFracxTipoFrac.Recuperar(tarifa.FEfecto))

        Select Case colFraccionamientos.Count
            Case Is = 0
                Throw New ApplicationException("Se debía haber recuperado al menos un fraccionamiento")
            Case Is = 1
                fracRV = colFraccionamientos.Item(0)
            Case Else
                Throw New ApplicationException("Se debía haber recuperado SOLO un fraccionamiento activo")
        End Select

        If fracRV.Cobertura.GUID <> Me.mCobertura.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la cobertura del fraccionamiento y del recuperador de valor debieran ser iguales")
        End If

        mValorCacheado = fracRV

        Return fracRV.Valor
    End Function

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

#End Region


End Class


<Serializable()> _
Public Class ColFraccionamientoRVSVDN
    Inherits ArrayListValidable(Of FraccionamientoRVSVDN)

#Region "Métodos"

    Public Function Recuperar(ByVal pCobertura As FN.Seguros.Polizas.DN.CoberturaDN) As ColFraccionamientoRVSVDN
        Dim col As New ColFraccionamientoRVSVDN()
        For Each fracRVSV As FraccionamientoRVSVDN In Me
            If fracRVSV.Cobertura.GUID = pCobertura.GUID Then
                col.Add(fracRVSV)
            End If
        Next

        Return col
    End Function

    Public Function Recuperar(ByVal nombreCobertura As String) As ColFraccionamientoRVSVDN
        Dim col As New ColFraccionamientoRVSVDN()
        For Each fracRVSV As FraccionamientoRVSVDN In Me
            If fracRVSV.Cobertura.Nombre = nombreCobertura Then
                col.Add(fracRVSV)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarColFracRV() As ColFraccionamientoRVDN

        Dim col As New ColFraccionamientoRVDN

        For Each fracRVSV As FraccionamientoRVSVDN In Me
            col.AddRange(fracRVSV.ColFraccionamientoRV)
        Next

        Return col
    End Function

#End Region

End Class