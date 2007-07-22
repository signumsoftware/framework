Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionFracRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

#Region "Atributos"

    Protected mValorResultadoOpr As Double
    Protected mValorResultadoISVprecedente As Double
    Protected mValorFraccionamiento As Double

    Protected mGUIDCobertura As String
    Protected mNombreCobertura As String

    Protected mGUIDFraccionamiento As String
    Protected mNombreFraccionamiento As String

    Protected mFraccionable As Boolean

    Protected mTipoOperador As String
    Protected mAplicado As Boolean
    Protected mGUIDTarifa As String

    Protected mOrdenAplicacion As Integer

#End Region

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        mGUIDTarifa = pTarifa.GUID
    End Sub

#End Region

#Region "Propiedades"

    Public Property ValorFraccionamiento() As Double
        Get
            Return mValorFraccionamiento
        End Get
        Set(ByVal value As Double)
            mValorFraccionamiento = value
        End Set
    End Property

    Public Property NombreFraccionamiento() As String
        Get
            Return mNombreFraccionamiento
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreFraccionamiento)
        End Set
    End Property

    Public Property GUIDFraccionamiento() As String
        Get
            Return mGUIDFraccionamiento
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDFraccionamiento)
        End Set
    End Property

    Public Property NombreCobertura() As String
        Get
            Return mNombreCobertura
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreCobertura)
        End Set
    End Property

    Public Property OrdenAplicacion() As Integer
        Get
            Return mOrdenAplicacion
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mOrdenAplicacion)
        End Set
    End Property

    Public Property ValorResultadoISVprecedente() As Double
        Get
            Return mValorResultadoISVprecedente
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorResultadoISVprecedente)
        End Set
    End Property

#End Region

#Region "Propiedades IOperacionCausaRVCacheDN"

    Public Property Aplicado() As Boolean Implements IOperacionCausaRVCacheDN.Aplicado
        Get
            Return Me.mAplicado
        End Get
        Set(ByVal value As Boolean)
            mAplicado = value
        End Set
    End Property

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.CoberturaDN), "0", mGUIDCobertura))
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.TarifaDN), "0", mGUIDTarifa))

            Return col
        End Get
    End Property

    Public Property Fraccionable() As Boolean Implements IOperacionCausaRVCacheDN.Fraccionable
        Get
            Return mFraccionable
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mFraccionable)
        End Set
    End Property

    Public Property GUIDCobertura() As String Implements IOperacionCausaRVCacheDN.GUIDCobertura
        Get
            Return mGUIDCobertura
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDCobertura)
        End Set
    End Property

    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Return Me.mGUIDCobertura & "/" & Me.mGUIDTarifa & "/" & Me.mGUIDFraccionamiento
        End Get
    End Property

    Public Property GUIDTarifa() As String Implements IOperacionCausaRVCacheDN.GUIDTarifa
        Get
            Return mGUIDTarifa
        End Get
        Set(ByVal value As String)
            mGUIDTarifa = value
        End Set
    End Property

    Public Property TipoOperador() As String Implements IOperacionCausaRVCacheDN.TipoOperador
        Get
            Return Me.mTipoOperador
        End Get
        Set(ByVal value As String)
            mTipoOperador = value
        End Set
    End Property

    Public Property ValorresultadoOpr() As Double Implements IOperacionCausaRVCacheDN.ValorresultadoOpr
        Get
            Return mValorResultadoOpr
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorResultadoOpr)
        End Set
    End Property

#End Region

#Region "Métodos IOperacionCausaRVCacheDN"

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements IOperacionCausaRVCacheDN.AsignarEntidadReferida
        Dim miguid As String = Me.mGUID
        MyBase.AsignarEntidadReferida(pEntidad)
        Me.mGUID = miguid

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = pEntidad
        Dim isvPrecedente As Framework.Operaciones.OperacionesDN.ISuministradorValorDN
        Dim fracRV As FraccionamientoRVDN
        Dim fracRVSV As FraccionamientoRVSVDN

        mValorResultadoOpr = op.ValorCacheado

        ' asignacion de las variables
        If TypeOf op.Operando1 Is ModuladorRVSVDN Then
            fracRVSV = op.Operando1
            If TypeOf op.Operando2 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando2
            End If
        Else
            fracRVSV = op.Operando2
            If TypeOf op.Operando1 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando1
            End If
        End If

        fracRV = fracRVSV.ValorCacheado

        mValorResultadoISVprecedente = isvPrecedente.ValorCacheado
        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        mGUIDCobertura = fracRVSV.Cobertura.GUID
        mNombreCobertura = fracRVSV.Cobertura.Nombre
        mOrdenAplicacion = op.OrdenOperacion

        If fracRV Is Nothing Then
            mAplicado = False
        Else
            mValorFraccionamiento = fracRV.Valor
            mGUIDFraccionamiento = fracRV.Fraccionamiento.GUID
            mNombreFraccionamiento = fracRV.Fraccionamiento.Nombre
            mFraccionable = fracRV.Fraccionable
            mAplicado = True
        End If

    End Sub

#End Region

    Public Property GUIDReferida1() As String Implements IOperacionCache.GUIDReferida
        Get
            Return Me.GUIDReferida
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException("Propiedad solo lectura")
        End Set
    End Property
End Class


<Serializable()> _
Public Class ColOperacionFracRVCacheDN
    Inherits ArrayListValidable(Of OperacionFracRVCacheDN)

#Region "Métodos"

    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionFracRVCacheDN
        Dim col As New ColOperacionFracRVCacheDN

        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next

        Return col
    End Function

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionFracRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub

    Public Function CalcularImporteTotal() As Double
        For Each oper As OperacionFracRVCacheDN In Me
            CalcularImporteTotal += oper.ValorresultadoOpr
        Next
    End Function

    Public Function RecuperarColUltimasOperaciones() As ColOperacionFracRVCacheDN
        Dim htPosicionesMaximas As New Collections.Generic.Dictionary(Of String, Double)
        ' clacuar el valor máximo para cada categoria

        For Each opIC As OperacionFracRVCacheDN In Me

            Dim clave As String = opIC.GUIDCobertura
            If htPosicionesMaximas.ContainsKey(clave) Then
                If htPosicionesMaximas(clave) < opIC.OrdenAplicacion Then
                    htPosicionesMaximas(clave) = opIC.OrdenAplicacion
                    'esmaximo = True
                End If
            Else
                htPosicionesMaximas(clave) = opIC.OrdenAplicacion
                ' esmaximo = True
            End If

        Next

        Dim col As New ColOperacionFracRVCacheDN
        For Each clave As String In htPosicionesMaximas.Keys
            Dim guids As String() = clave.Split("/")
            col.Add(Me.Recuperar(htPosicionesMaximas(clave), guids(0)))
        Next

        Return col

    End Function

    Public Function Recuperar(ByVal pOrdenAplicacion As Double, ByVal pGUIDCobertura As String) As OperacionFracRVCacheDN

        For Each opIC As OperacionFracRVCacheDN In Me
            If pOrdenAplicacion = opIC.OrdenAplicacion AndAlso opIC.GUIDCobertura = pGUIDCobertura Then
                Return opIC
            End If
        Next

        Return Nothing
    End Function

#End Region

End Class