Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionModuladorRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

    Protected mValorresultadoOpr As Double
    Protected mValorresultadoISVprecedente As Double
    Protected mValorresultadoModulador As Double

    Protected mGUIDCobertura As String
    Protected mNombreCobertura As String

    Protected mGUIDModulador As String
    Protected mNombreModulador As String

    Protected mGUIDCaracteristica As String
    Protected mNombreCaracteristica As String

    Protected mGUIDCategoria As String
    Protected mNombreCategoria As String


    Protected mTipoOperador As String
    Protected mAplicado As Boolean
    Protected mGUIDTarifa As String

    Protected mOrdenAplicacion As Integer

    Public Property OrdenAplicacion() As Integer
        Get
            Return mOrdenAplicacion
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mOrdenAplicacion)
        End Set
    End Property

    Public Property ValorresultadoModulador() As Double
        Get
            Return mValorresultadoModulador
        End Get
        Set(ByVal value As Double)
            mValorresultadoModulador = value
        End Set
    End Property


    Public Property NombreModulador() As String

        Get
            Return mNombreModulador
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreModulador)

        End Set
    End Property

    Public Property GUIDModulador() As String

        Get
            Return mGUIDModulador
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDModulador)

        End Set
    End Property




    Public Property NombreCaracteristica() As String

        Get
            Return mNombreCaracteristica
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNombreCaracteristica)

        End Set
    End Property

    Public Property GUIDCaracteristica() As String

        Get
            Return mGUIDCaracteristica
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGUIDCaracteristica)

        End Set
    End Property



    Public Sub New()

    End Sub

    Public Sub New(ByVal pOp As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
        MyBase.New(pOp, HuellaEntidadDNIntegridadRelacional.ninguna)
        mGUIDTarifa = pTarifa.GUID
    End Sub

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
        Dim isvPrecedente As Framework.Operaciones.OperacionesDN.ISuministradorValorDN = Nothing
        Dim miModuladorRV As ModuladorRVDN
        Dim miModuladorRVSV As ModuladorRVSVDN


        mValorresultadoOpr = op.ValorCacheado

        ' asignacion de las variables
        If TypeOf op.Operando1 Is ModuladorRVSVDN Then
            miModuladorRVSV = op.Operando1
            If TypeOf op.Operando2 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando2
            End If
        Else
            miModuladorRVSV = op.Operando2
            If TypeOf op.Operando1 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando1
            End If
        End If

        miModuladorRV = miModuladorRVSV.ValorCacheado

        If isvPrecedente IsNot Nothing Then
            mValorresultadoISVprecedente = isvPrecedente.ValorCacheado
        End If

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre


        mGUIDCobertura = miModuladorRVSV.Cobertura.GUID
        mNombreCobertura = miModuladorRVSV.Cobertura.Nombre

        mGUIDCaracteristica = miModuladorRVSV.Caracteristica.GUID
        mNombreCaracteristica = miModuladorRVSV.Caracteristica.Nombre

        mOrdenAplicacion = op.OrdenOperacion

        If miModuladorRV Is Nothing Then

            mAplicado = False

        Else
            mValorresultadoModulador = miModuladorRV.ValorCacheado

            mGUIDModulador = miModuladorRV.Modulador.GUID
            mNombreModulador = miModuladorRV.Modulador.Nombre

            mGUIDCategoria = miModuladorRV.Categoria.GUID
            mNombreCategoria = miModuladorRV.Categoria.Nombre
            mAplicado = True
        End If


    End Sub

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


    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.CoberturaDN), "0", mGUIDCobertura))
            col.Add(New HEDN(GetType(FN.Seguros.Polizas.DN.TarifaDN), "0", mGUIDTarifa))
            col.Add(New HEDN(GetType(FN.RiesgosVehiculos.DN.CategoriaDN), "0", mGUIDCategoria))
            col.Add(New HEDN(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN), "0", mGUIDCaracteristica))


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
            Return Me.mGUIDCategoria & "/" & Me.mGUIDCobertura & "/" & Me.mGUIDTarifa & "/" & Me.mGUIDCaracteristica & "/" & Me.mGUIDModulador
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
Public Class ColOperacionModuladorRVCacheDN
    Inherits ArrayListValidable(Of OperacionModuladorRVCacheDN)

#Region "Métodos"
    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionModuladorRVCacheDN

        Dim col As New ColOperacionModuladorRVCacheDN


        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next


        Return col

    End Function

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionModuladorRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub

    Public Function CalcularImporteTotal() As Double



        For Each oper As OperacionModuladorRVCacheDN In Me
            CalcularImporteTotal += oper.ValorresultadoOpr
        Next

    End Function





    Public Function RecuperarColUltimasOperaciones() As ColOperacionModuladorRVCacheDN


        Dim htPosicionesMaximas As New Collections.Generic.Dictionary(Of String, Double)
        ' clacuar el valor maximo para cada categoria

        For Each opIC As OperacionModuladorRVCacheDN In Me

            Dim clave As String = opIC.GUIDCategoria & "/" & opIC.GUIDCobertura
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



        Dim col As New ColOperacionModuladorRVCacheDN
        For Each clave As String In htPosicionesMaximas.Keys
            Dim guids As String() = clave.Split("/")
            col.Add(Me.Recuperar(htPosicionesMaximas(clave), guids(0), guids(1)))
        Next


        Return col

    End Function





    Public Function Recuperar(ByVal pOrdenAplicacion As Double, ByVal pGUIDCategoria As String, ByVal pGUIDCobertura As String) As OperacionModuladorRVCacheDN

        For Each opIC As OperacionModuladorRVCacheDN In Me
            If pOrdenAplicacion = opIC.OrdenAplicacion AndAlso opIC.GUIDCategoria = pGUIDCategoria AndAlso opIC.GUIDCobertura = pGUIDCobertura Then
                Return opIC
            End If
        Next

        Return Nothing

    End Function

#End Region

End Class




