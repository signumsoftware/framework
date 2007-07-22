Imports Framework.DatosNegocio



<Serializable()> _
Public Class OperacionImpuestoRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

    Protected mFraccionable As Boolean
    Protected mValorImpuesto As Double
    Protected mValorPrimaNeta As Double
    Protected mValorresultadoOpr As Double

    Protected mGUIDCobertura As String
    Protected mNombreCobertura As String

    Protected mGUIDImpuesto As String
    Protected mNombreImpuesto As String

    Protected mTipoOperador As String
    Protected mAplicado As Boolean
    Protected mGUIDTarifa As String

    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        mGUIDTarifa = pTarifa.GUID
    End Sub


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

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements IOperacionCausaRVCacheDN.AsignarEntidadReferida
        Dim miguid As String = Me.mGUID
        MyBase.AsignarEntidadReferida(pEntidad)
        Me.mGUID = miguid

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = pEntidad
        Dim opPrimaneta As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim miImpuestoRV As ImpuestoRVDN
        Dim miImpuestoRVSV As ImpuestoRVSVDN


        mValorresultadoOpr = op.ValorCacheado

        ' asignacion de las variables
        If TypeOf op.Operando1 Is ImpuestoRVSVDN Then
            miImpuestoRVSV = op.Operando1

            If TypeOf op.Operando2 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                opPrimaneta = op.Operando2


            End If
        Else
            miImpuestoRVSV = op.Operando2
            If TypeOf op.Operando1 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                opPrimaneta = op.Operando1


            End If
        End If
        miImpuestoRV = miImpuestoRVSV.ValorCacheado

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre


        If Not opPrimaneta Is Nothing Then
            mValorPrimaNeta = opPrimaneta.ValorCacheado
        End If

        mGUIDCobertura = miImpuestoRVSV.Cobertura.GUID
        mNombreCobertura = miImpuestoRVSV.Cobertura.Nombre
        mGUIDImpuesto = miImpuestoRVSV.Impuesto.GUID
        mNombreImpuesto = miImpuestoRVSV.Impuesto.Nombre
        Me.mFraccionable = miImpuestoRVSV.Impuesto.Fraccionable

        If Not miImpuestoRV Is Nothing Then ' es posible que la cobertura no presente impuesto
            mValorImpuesto = miImpuestoRV.Valor
            mAplicado = True
        Else
            mAplicado = False
        End If






    End Sub


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
            Return mValorresultadoOpr
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorresultadoOpr)

        End Set
    End Property

    Public Property ValorPrimaneta() As Double

        Get
            Return mValorPrimaNeta
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorPrimaNeta)

        End Set
    End Property

    Public Property ValorImpuesto() As Double

        Get
            Return mValorImpuesto
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorImpuesto)

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


    Public Property NombreImpuesto() As String

        Get
            Return mNombreImpuesto
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreImpuesto)

        End Set
    End Property

    Public Property GUIDImpuesto() As String

        Get
            Return mGUIDImpuesto
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDImpuesto)

        End Set
    End Property



    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Return Me.GUIDImpuesto & "/" & Me.GUIDReferida & "/" & Me.GUIDTarifa
        End Get
    End Property

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.RiesgosVehiculos.DN.ImpuestoDN), "0", mGUIDImpuesto))

            Return col
        End Get
    End Property

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
Public Class ColOperacionImpuestoRVCacheDN
    Inherits ArrayListValidable(Of OperacionImpuestoRVCacheDN)

#Region "Métodos"

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionImpuestoRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub


    Public Function CalcularTotalesXImpuesto(ByVal pFraccionable As Fraccionable) As System.Collections.Generic.Dictionary(Of String, Double)

        Dim ht As New System.Collections.Generic.Dictionary(Of String, Double)

        For Each oper As OperacionImpuestoRVCacheDN In Me

            If pFraccionable = Fraccionable.Todos OrElse (pFraccionable = Fraccionable.SI AndAlso oper.Fraccionable) OrElse (pFraccionable = Fraccionable.No AndAlso Not oper.Fraccionable) Then
                If ht.ContainsKey(oper.GUIDImpuesto) Then
                    ht(oper.GUIDImpuesto) = +oper.ValorImpuesto
                Else
                    ht.Add(oper.GUIDImpuesto, oper.ValorImpuesto)
                End If
            End If

        Next

        Return ht

    End Function

    Public Function CalcularImporteTotal(ByVal pFraccionable As Fraccionable) As Double

        Dim ht As System.Collections.Generic.Dictionary(Of String, Double) = CalcularTotalesXImpuesto(pFraccionable)
        For Each valor As Double In ht.Values
            CalcularImporteTotal += valor
        Next

    End Function


    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionImpuestoRVCacheDN

        Dim col As New ColOperacionImpuestoRVCacheDN


        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next

        Return col


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

Public Enum Fraccionable
    SI
    No
    Todos
End Enum




