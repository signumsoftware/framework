Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionSumaRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

    Protected mValorresultadoOpr As Double
    Protected mValorresultadoOp1 As Double
    Protected mValorresultadoOP2 As Double
    Protected mGUIDOperacion As String
    Protected mNombreOperacion As String


    Protected mTipoOperador As String
    Protected mGUIDTarifa As String




    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        mGUIDTarifa = pTarifa.GUID
    End Sub


    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Dim miguid As String = Me.mGUID
        MyBase.AsignarEntidadReferida(pEntidad)
        Me.mGUID = miguid

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = pEntidad
        Dim isvOp1, isvOp2 As Framework.Operaciones.OperacionesDN.ISuministradorValorDN


        mValorresultadoOpr = op.ValorCacheado
        Me.mNombreOperacion = op.Nombre
        Me.mGUIDOperacion = op.GUID
        isvOp1 = op.Operando1
        isvOp2 = op.Operando2


        Me.mValorresultadoOp1 = isvOp1.ValorCacheado
        Me.mValorresultadoOP2 = isvOp2.ValorCacheado

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        Me.EliminarEntidadReferida()

    End Sub



    Public Property GUIDTarifa() As String
        Get
            Return mGUIDTarifa
        End Get
        Set(ByVal value As String)
            mGUIDTarifa = value
        End Set
    End Property

    Public Property NombreOperacion() As String

        Get
            Return mNombreOperacion
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreOperacion)

        End Set
    End Property

    Public Property GUIDOperacion() As String

        Get
            Return mGUIDOperacion
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDOperacion)

        End Set
    End Property

    Public Property TipoOperador() As String
        Get
            Return Me.mTipoOperador
        End Get
        Set(ByVal value As String)
            mTipoOperador = value
        End Set
    End Property

    Public Property ValorresultadoOpr() As Double

        Get
            Return mValorresultadoOpr
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorresultadoOpr)

        End Set
    End Property

    Public Property ValorresultadoOp1() As Double

        Get
            Return Me.mValorresultadoOp1
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorresultadoOp1)

        End Set
    End Property

    Public Property ValorresultadoOp2() As Double

        Get
            Return Me.mValorresultadoOP2
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorresultadoOP2)

        End Set
    End Property




    Public Property GUIDReferida1() As String Implements IOperacionCache.GUIDReferida
        Get
            Return Me.GUIDReferida
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException("Propiedad solo lectura")
        End Set
    End Property

    Public Property Aplicado() As Boolean Implements IOperacionCausaRVCacheDN.Aplicado
        Get
            Throw New NotImplementedException
        End Get
        Set(ByVal value As Boolean)
            Throw New NotImplementedException
        End Set
    End Property

    Public Sub AsignarEntidadReferida1(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements IOperacionCausaRVCacheDN.AsignarEntidadReferida
        Me.AsignarEntidadReferida(pEntidad)
    End Sub

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Property Fraccionable() As Boolean Implements IOperacionCausaRVCacheDN.Fraccionable
        Get
            Throw New NotImplementedException
        End Get
        Set(ByVal value As Boolean)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property GUIDCobertura() As String Implements IOperacionCausaRVCacheDN.GUIDCobertura
        Get
            Throw New NotImplementedException
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException
        End Set
    End Property

    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Property GUIDTarifa1() As String Implements IOperacionCausaRVCacheDN.GUIDTarifa
        Get
            Return Me.GUIDTarifa
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property TipoOperador1() As String Implements IOperacionCausaRVCacheDN.TipoOperador
        Get
            Return Me.TipoOperador
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property ValorresultadoOpr1() As Double Implements IOperacionCausaRVCacheDN.ValorresultadoOpr
        Get
            Return ValorresultadoOpr
        End Get
        Set(ByVal value As Double)
            Throw New NotImplementedException
        End Set
    End Property

End Class





<Serializable()> _
Public Class ColOperacionSumaRVCacheDN
    Inherits ArrayListValidable(Of OperacionSumaRVCacheDN)

#Region "Métodos"

    Public Function RecuperarValorOperacionxNombreOperacion(ByVal texto As String) As Double
        For Each opSRVC As OperacionSumaRVCacheDN In Me
            If opSRVC.NombreOperacion.Contains(texto) Then
                Return opSRVC.ValorresultadoOpr
            End If
        Next

        Return Nothing
    End Function

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionSumaRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub

#End Region

End Class




