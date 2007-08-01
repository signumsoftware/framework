Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionComisionRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)

    Implements IOperacionCausaRVCacheDN


    Protected mValorResultadoOpr As Double
    Protected mValorResultadoISVprecedente As Double
    Protected mValorResultadoComision As Double

    Protected mGUIDComision As String
    Protected mNombreComision As String

    Protected mGUIDCobertura As String
    Protected mNombreCobertura As String

    Protected mTipoOperador As String
    Protected mAplicado As Boolean
    Protected mGUIDTarifa As String

    Protected mFraccionable As Boolean
    Protected mOrdenAplicacion As Integer


    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        Me.AsignarEntidadReferida(pOp)
        mGUIDTarifa = pTarifa.GUID
    End Sub


    Public Property GUIDReferida1() As String Implements IOperacionCache.GUIDReferida
        Get
            Return Me.GUIDReferida
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException("Propiedad solo lectura")
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

    Public Property Fraccionable() As Boolean Implements IOperacionCausaRVCacheDN.Fraccionable

        Get
            Return mFraccionable
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mFraccionable)

        End Set
    End Property

    Public Property GUIDTarifa() As String Implements IOperacionCausaRVCacheDN.GUIDTarifa
        Get
            Return mGUIDTarifa
        End Get
        Set(ByVal value As String)
            mGUIDTarifa = value
        End Set
    End Property

    Public Property Aplicado() As Boolean Implements IOperacionCausaRVCacheDN.Aplicado
        Get
            Return Me.mAplicado
        End Get
        Set(ByVal value As Boolean)
            mAplicado = value
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

    Public Property GUIDCobertura() As String Implements IOperacionCausaRVCacheDN.GUIDCobertura

        Get
            Return mGUIDCobertura
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDCobertura)

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

    Public Property NombreComision() As String
        Get
            Return mNombreComision
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreComision)
        End Set
    End Property

    Public Property GUIDComision() As String
        Get
            Return mGUIDComision
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDComision)
        End Set
    End Property

    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Return Me.mGUIDComision & "/" & Me.mGUIDReferida & "/" & Me.mGUIDTarifa
        End Get
    End Property

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.RiesgosVehiculos.DN.ComisionDN), "", mGUIDComision))

            Return col
        End Get
    End Property

    Public Property ValorResultadoComision() As Double
        Get
            Return mValorResultadoComision
        End Get
        Set(ByVal value As Double)
            mValorResultadoComision = value
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

    Public Property ValorResultadoISVprecedente() As Double
        Get
            Return mValorResultadoISVprecedente
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorResultadoISVprecedente)
        End Set
    End Property

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements IOperacionCausaRVCacheDN.AsignarEntidadReferida
        Dim miguid As String = Me.mGUID
        MyBase.AsignarEntidadReferida(pEntidad)
        Me.mGUID = miguid

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = pEntidad
        Dim isvPrecedente As Framework.Operaciones.OperacionesDN.ISuministradorValorDN = Nothing
        Dim miComisionRV As ComisionRVDN = Nothing
        Dim miComisionRVSV As ComisionRVSVDN

        mValorResultadoOpr = op.ValorCacheado

        ' asignacion de las variables
        If TypeOf op.Operando1 Is ComisionRVSVDN Then
            miComisionRVSV = op.Operando1
            If TypeOf op.Operando2 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando2
            End If
        Else
            miComisionRVSV = op.Operando2
            If TypeOf op.Operando1 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando1
            End If
        End If

        If isvPrecedente IsNot Nothing Then
            mValorResultadoISVprecedente = isvPrecedente.ValorCacheado
        End If

        miComisionRV = miComisionRVSV.ValorCacheado

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        mGUIDCobertura = miComisionRVSV.Cobertura.GUID
        mNombreCobertura = miComisionRVSV.Cobertura.Nombre

        mOrdenAplicacion = op.OrdenOperacion

        If miComisionRV Is Nothing Then
            mAplicado = False
        Else
            mValorResultadoComision = miComisionRV.Valor

            mGUIDComision = miComisionRV.Comision.GUID
            mNombreComision = miComisionRV.Comision.Nombre

            mAplicado = True
        End If

        miComisionRV = miComisionRVSV.ValorCacheado

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        Me.mFraccionable = miComisionRVSV.Comision.Fraccionable

    End Sub

End Class




<Serializable()> _
Public Class ColOperacionComisionRVCacheDN
    Inherits ArrayListValidable(Of OperacionComisionRVCacheDN)

#Region "Métodos"

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionComisionRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub


    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionComisionRVCacheDN

        Dim col As New ColOperacionComisionRVCacheDN

        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next

        Return col


    End Function

    Public Function CalcularTotalesXComision(ByVal pFraccionable As Fraccionable) As System.Collections.Generic.Dictionary(Of String, Double)

        Dim ht As New System.Collections.Generic.Dictionary(Of String, Double)

        For Each oper As OperacionComisionRVCacheDN In Me

            If pFraccionable = Fraccionable.Todos OrElse (pFraccionable = Fraccionable.SI AndAlso oper.Fraccionable) OrElse (pFraccionable = Fraccionable.No AndAlso Not oper.Fraccionable) Then
                If ht.ContainsKey(oper.GUIDComision) Then
                    ht(oper.GUIDComision) = +oper.ValorResultadoComision
                Else
                    ht.Add(oper.GUIDComision, oper.ValorResultadoComision)
                End If
            End If

        Next

        Return ht

    End Function

    Public Function CalcularImporteTotal(ByVal pFraccionable As Fraccionable) As Double

        Dim ht As System.Collections.Generic.Dictionary(Of String, Double) = CalcularTotalesXComision(pFraccionable)
        For Each valor As Double In ht.Values
            CalcularImporteTotal += valor
        Next

    End Function

    Public Function RecuperarCausas() As Framework.DatosNegocio.ColHEDN
        Dim col As New Framework.DatosNegocio.ColHEDN
        For Each dd As IOperacionCausaRVCacheDN In Me
            col.AddRangeObject(dd.ColHeCausas)
        Next

        Return col

    End Function
#End Region

End Class






