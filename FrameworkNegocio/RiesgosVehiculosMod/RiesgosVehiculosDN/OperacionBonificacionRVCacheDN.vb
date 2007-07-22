Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionBonificacionRVCacheDN
    Inherits Framework.DatosNegocio.HETCacheableDN(Of Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)
    Implements IOperacionCausaRVCacheDN

#Region "Atributos"

    Protected mValorResultadoOpr As Double
    Protected mValorResultadoISVprecedente As Double
    Protected mValorResultadoBonificacion As Double

    Protected mGUIDBonificacion As String
    Protected mNombreBonificacion As String

    Protected mGUIDCategoria As String
    Protected mNombreCategoria As String

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
        Me.AsignarEntidadReferida(pOp)
        mGUIDTarifa = pTarifa.GUID
    End Sub

#End Region

#Region "Propiedades"

    Public Property OrdenAplicacion() As Integer
        Get
            Return mOrdenAplicacion
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mOrdenAplicacion)
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

#End Region

#Region "Propiedades IOperacionCausaRVCacheDN"

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
            Return Me.mAplicado
        End Get
        Set(ByVal value As Boolean)
            mAplicado = value
        End Set
    End Property

    Public ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN Implements IOperacionCausaRVCacheDN.ColHeCausas
        Get
            Dim col As New Framework.DatosNegocio.ColHEDN
            col.Add(New HEDN(GetType(FN.RiesgosVehiculos.DN.ComisionDN), "", mGUIDBonificacion))

            Return col
        End Get
    End Property

    Public Property Fraccionable() As Boolean Implements IOperacionCausaRVCacheDN.Fraccionable
        Get
            Return True
        End Get
        Set(ByVal value As Boolean)
            Throw New NotImplementedException("La propiedad fraccionable es de solo lectura")
        End Set
    End Property

    Public Property GUIDCobertura() As String Implements IOperacionCausaRVCacheDN.GUIDCobertura
        Get
            Throw New NotImplementedException("Esta propiedad no se aplica a la bonificación")
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException("Esta propiedad no se aplica a la bonificación")
        End Set
    End Property

    Public ReadOnly Property GUIDsCausas() As String Implements IOperacionCausaRVCacheDN.GUIDsCausas
        Get
            Return Me.mGUIDBonificacion & "/" & Me.mGUIDReferida & "/" & Me.mGUIDTarifa
        End Get
    End Property

    Public Property GUIDTarifa() As String Implements IOperacionCausaRVCacheDN.GUIDTarifa
        Get
            Return Me.mTipoOperador
        End Get
        Set(ByVal value As String)
            mTipoOperador = value
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
        Dim isvPrecedente As Framework.Operaciones.OperacionesDN.ISuministradorValorDN = Nothing
        Dim miBonificacionRV As BonificacionRVDN
        Dim miBonificacionRVSV As BonificacionRVSVDN

        mValorresultadoOpr = op.ValorCacheado

        ' asignacion de las variables
        If TypeOf op.Operando1 Is BonificacionRVSVDN Then
            miBonificacionRVSV = op.Operando1
            If TypeOf op.Operando2 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando2
            End If
        Else
            miBonificacionRVSV = op.Operando2
            If TypeOf op.Operando1 Is Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN Then
                isvPrecedente = op.Operando1
            End If
        End If

        miBonificacionRV = miBonificacionRVSV.ValorCacheado

        If isvPrecedente IsNot Nothing Then
            mValorresultadoISVprecedente = isvPrecedente.ValorCacheado
        End If

        mTipoOperador = CType(op.IOperadorDN, IEntidadBaseDN).Nombre

        mOrdenAplicacion = op.OrdenOperacion

        If miBonificacionRV Is Nothing Then
            mAplicado = False
        Else
            mValorResultadoBonificacion = miBonificacionRV.Valor

            mGUIDBonificacion = miBonificacionRV.Bonificacion.GUID
            mNombreBonificacion = miBonificacionRV.Bonificacion.Nombre

            mGUIDCategoria = miBonificacionRV.Categoria.GUID
            mNombreCategoria = miBonificacionRV.Categoria.Nombre

            mAplicado = True
        End If

    End Sub

#End Region

End Class


<Serializable()> _
Public Class ColOperacionBonificacionRVCacheDN
    Inherits ArrayListValidable(Of OperacionBonificacionRVCacheDN)

#Region "Métodos"

    Public Function RecuperarxGUIDCausa(ByVal pGUIDCausa As String) As ColOperacionBonificacionRVCacheDN
        Dim col As New ColOperacionBonificacionRVCacheDN

        For Each dd As IOperacionCausaRVCacheDN In Me
            If dd.GUIDsCausas.Contains(pGUIDCausa) Then
                col.Add(dd)
            End If
        Next

        Return col
    End Function

    Public Sub LimpiarEntidadesReferidas()
        For Each opIC As OperacionBonificacionRVCacheDN In Me
            opIC.EliminarEntidadReferida()
        Next
    End Sub

    Public Function CalcularImporteTotal() As Double
        For Each oper As OperacionBonificacionRVCacheDN In Me
            CalcularImporteTotal += oper.ValorresultadoOpr
        Next
    End Function

    Public Function RecuperarColUltimasOperaciones() As ColOperacionBonificacionRVCacheDN
        Dim htPosicionesMaximas As New Collections.Generic.Dictionary(Of String, Double)
        ' calcular el valor máximo para cada categoria

        For Each opIC As OperacionBonificacionRVCacheDN In Me

            Dim clave As String = opIC.GUIDCategoria
            If htPosicionesMaximas.ContainsKey(clave) Then
                If htPosicionesMaximas(clave) < opIC.OrdenAplicacion Then
                    htPosicionesMaximas(clave) = opIC.OrdenAplicacion
                End If
            Else
                htPosicionesMaximas(clave) = opIC.OrdenAplicacion
            End If

        Next

        Dim col As New ColOperacionBonificacionRVCacheDN
        For Each clave As String In htPosicionesMaximas.Keys
            Dim guids As String() = clave.Split("/")
            col.Add(Me.Recuperar(htPosicionesMaximas(clave), guids(0)))
        Next

        Return col

    End Function

    Public Function Recuperar(ByVal pOrdenAplicacion As Double, ByVal pGUIDCategoria As String) As OperacionBonificacionRVCacheDN

        For Each opIC As OperacionBonificacionRVCacheDN In Me
            If pOrdenAplicacion = opIC.OrdenAplicacion AndAlso opIC.GUIDCategoria = pGUIDCategoria Then
                Return opIC
            End If
        Next

        Return Nothing

    End Function

#End Region

End Class