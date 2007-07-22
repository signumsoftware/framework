Imports Framework.DatosNegocio
<Serializable()> _
Public Class OperacionPrimaBaseRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

    Protected mValorPrimabase As Double
    Protected mValorOperacion As Double

    Protected mGUIDCobertura As String
    Protected mNombreCobertura As String

    Protected mGUIDCategoria As String
    Protected mNombreCategoria As String

    Protected mTipoOperador As String
    Protected mGUIDTarifa As String


    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        mGUIDTarifa = pTarifa.GUID
    End Sub


    Public Property ValorOperacion() As Double Implements IOperacionCausaRVCacheDN.ValorresultadoOpr
        Get
            Return mValorOperacion
        End Get
        Set(ByVal value As Double)
            mValorOperacion = value
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


    Public Property TipoOperador() As String Implements IOperacionCausaRVCacheDN.TipoOperador
        Get
            Return Me.mTipoOperador
        End Get
        Set(ByVal value As String)
            mTipoOperador = value
        End Set
    End Property

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements IOperacionCausaRVCacheDN.AsignarEntidadReferida
        Dim miguid As String = Me.mGUID
        MyBase.AsignarEntidadReferida(pEntidad)
        Me.mGUID = miguid

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = pEntidad

        Dim pb As FN.RiesgosVehiculos.DN.PrimaBaseRVDN
        Dim PrimabaseRVSV As FN.RiesgosVehiculos.DN.PrimabaseRVSVDN

        ' asignacion de las variables
        If TypeOf op.Operando1 Is PrimabaseRVSVDN Then
            PrimabaseRVSV = op.Operando1
        Else
            PrimabaseRVSV = op.Operando2
        End If

        pb = PrimabaseRVSV.ValorCacheado

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        mGUIDCobertura = PrimabaseRVSV.Cobertura.GUID
        mNombreCobertura = PrimabaseRVSV.Cobertura.Nombre
        mValorOperacion = op.ValorCacheado

        If pb Is Nothing Then
            mValorPrimabase = 0

        Else
            mValorPrimabase = pb.Importe
            mGUIDCategoria = pb.Categoria.GUID
            mNombreCategoria = pb.Categoria.Nombre

        End If



    End Sub

    Public Property ValorPrimabase() As Double

        Get
            Return mValorPrimabase
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorPrimabase)

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


    Public Property NombreCategoria() As String

        Get
            Return mNombreCategoria
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreCategoria)

        End Set
    End Property

    Public Property GUIDCategoria() As String

        Get
            Return mGUIDCategoria
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDCategoria)

        End Set
    End Property






    Public Property Aplicado() As Boolean Implements IOperacionCausaRVCacheDN.Aplicado
        Get
            Return True
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.CoberturaDN), "0", mGUIDCobertura))
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.TarifaDN), "0", mGUIDTarifa))
            col.Add(New HEDN(GetType(FN.RiesgosVehiculos.DN.CategoriaDN), "0", mGUIDCategoria))


            Return col

        End Get
    End Property

    Public Property Fraccionable() As Boolean Implements IOperacionCausaRVCacheDN.Fraccionable
        Get
            Return True
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Return Me.mGUIDCategoria & "/" & Me.mGUIDCobertura & "/" & Me.mGUIDTarifa
        End Get
    End Property

    Public Property GUIDReferida1() As String Implements IOperacionCache.GUIDReferida
        Get
            Return Me.GUIDReferida
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException("Operación solo lectura")
        End Set
    End Property
End Class




<Serializable()> _
Public Class ColOperacionPrimaBaseRVCacheDN
    Inherits ArrayListValidable(Of OperacionPrimaBaseRVCacheDN)

#Region "Métodos"

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionPrimaBaseRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub
    Public Function RecuperarCausas() As Framework.DatosNegocio.ColHEDN


        Dim col As New Framework.DatosNegocio.ColHEDN
        For Each dd As IOperacionCausaRVCacheDN In Me
            col.AddRangeObject(dd.ColHeCausas)
        Next


        Return col

    End Function



    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionPrimaBaseRVCacheDN

        Dim col As New ColOperacionPrimaBaseRVCacheDN
        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next

        Return col



    End Function

#End Region

End Class





