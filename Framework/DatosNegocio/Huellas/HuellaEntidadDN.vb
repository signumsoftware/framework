
<Serializable()> _
Public MustInherit Class HETCacheableDN(Of T)
    Inherits HuellaEntidadCacheableDN

    Public Sub New()
        MyBase.New()
        ' Me.mTipoEntidadReferidaFullNme = GetType(T).FullName
        Me.AsignarDatosTipo(GetType(T))
    End Sub

    Public Sub New(ByVal pentidad As T, ByVal relacionIntegridad As HuellaEntidadDNIntegridadRelacional)

        MyBase.New(pentidad, relacionIntegridad)
        '   Me.mTipoEntidadReferidaFullNme = GetType(T).FullName

    End Sub
    Public Sub New(ByVal pentidad As T, ByVal relacionIntegridad As HuellaEntidadDNIntegridadRelacional, ByVal tostrinentidad As String)

        MyBase.New(pentidad, relacionIntegridad, tostrinentidad)
        '  Me.mTipoEntidadReferidaFullNme = GetType(T).FullName

    End Sub

    Public Overrides ReadOnly Property TipoEntidadReferida() As System.Type
        Get
            Return GetType(T)
        End Get
    End Property

    Public Function Iguales(ByVal ph As HuellaEntidadTipadaDN(Of T)) As Boolean
        If Me Is ph OrElse (Me.ID = ph.ID AndAlso Me.TipoEntidadReferida Is ph.TipoEntidadReferida) Then
            Return True
        Else
            Return False
        End If
    End Function


    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As IEntidadDN)
        Dim o As Object
        o = pEntidad
        If pEntidad Is Nothing Then
            Throw New ApplicationException("ERROR: Se produjo un error en la AsignarEntidadReferida la entidad no puede ser nothing")
        End If
        If GetType(T) Is o.GetType Then
            MyBase.AsignarEntidadReferida(pEntidad)
        Else
            Throw New ApplicationException("Los tipos no coinciden")
        End If


    End Sub

    
End Class

<Serializable()> _
Public MustInherit Class HuellaEntidadTipadaDN(Of T)
    Inherits HEDN

    Public Sub New()
        MyBase.New()
        Me.AsignarDatosTipo(GetType(T))
        'Me.mTipoEntidadReferidaFullName = .FullName
    End Sub
    Public Sub New(ByVal phe As HEDN)
        MyBase.New(phe.TipoEntidadReferida, phe.IdEntidadReferida, phe.GUIDReferida)
    End Sub

    Public Sub New(ByVal pentidad As T, ByVal relacionIntegridad As HuellaEntidadDNIntegridadRelacional)

        MyBase.New(pentidad, relacionIntegridad)
        ' Me.mTipoEntidadReferidaFullName = GetType(T).FullName

    End Sub
    Public Sub New(ByVal pentidad As T, ByVal relacionIntegridad As HuellaEntidadDNIntegridadRelacional, ByVal tostrinentidad As String)

        MyBase.New(pentidad, relacionIntegridad, tostrinentidad)
        ' Me.mTipoEntidadReferidaFullName = GetType(T).FullName

    End Sub


    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As IEntidadDN)
        MyBase.AsignarEntidadReferida(pEntidad)
        If pEntidad.ID Is Nothing OrElse pEntidad.ID = "" Then
            Me.mID = ""
        Else
            Me.mID = pEntidad.ID

        End If



    End Sub

    <RelacionPropCampoAtribute("mEntidadReferidaHuella")> _
    Public ReadOnly Property EntidadReferidaTipada() As T
        Get
            Return CType(Me.mEntidadReferidaHuella, Object)

        End Get
    End Property

    Public ReadOnly Property TipoFijadoEntidadReferida() As System.Type
        Get
            Return GetType(T)

        End Get
    End Property
    Public Function Iguales(ByVal ph As HuellaEntidadTipadaDN(Of T)) As Boolean
        If Me Is ph OrElse (Me.ID = ph.ID AndAlso Me.TipoEntidadReferida Is ph.TipoEntidadReferida) Then
            Return True
        Else
            Return False
        End If
    End Function

End Class


Public Enum HuellaEntidadDNIntegridadRelacional
    ninguna
    relacion
    relacionDebeExixtir

End Enum


<Serializable()> _
Public MustInherit Class HuellaEntidadCacheableDN
    Inherits HEDN

    Public Sub New()
        MyBase.New()
    End Sub


    Public Sub New(ByVal pentidad As IEntidadDN, ByVal pIntegridadRelacional As HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pentidad, pIntegridadRelacional)
    End Sub

    Public Sub New(ByVal pentidad As IEntidadDN, ByVal pIntegridadRelacional As HuellaEntidadDNIntegridadRelacional, ByVal pToStringEntidadReferida As String)
        MyBase.New(pentidad, pIntegridadRelacional, pToStringEntidadReferida)
    End Sub

    Public Overridable Sub ActualizarHuella(ByVal phuellaClonActualizado As HuellaEntidadCacheableDN)
        'Me.mID = phuellaClonActualizado.ID
        'Me.mIdEntidadReferida = phuellaClonActualizado.IdEntidadReferida
        'Me.mNombre = phuellaClonActualizado.Nombre
        'Me.mToStringEntidadReferida = phuellaClonActualizado.ToStringEntidadReferida
        'Me.mBaja = phuellaClonActualizado.Baja
        'Me.mFechaModificacionEntidadReferida = phuellaClonActualizado.FechaModificacionEntidadReferida
        'Me.mFechaModificacion = phuellaClonActualizado.FechaModificacion
        Me.mEntidadReferidaHuella = phuellaClonActualizado.EntidadReferida

    End Sub

End Class


''' <summary>Interfaz proporciona los datos mínimos para una huella de una EntidadDN.</summary>

Public Interface IHuellaEntidadDN
    Inherits IEntidadDN
#Region "Propiedades"

    ReadOnly Property IdEntidadReferida() As String
    ReadOnly Property TipoEntidadReferidaFullNme() As String
    ReadOnly Property ToStringEntidadReferida() As String
    ReadOnly Property IntegridadRelacional() As HuellaEntidadDNIntegridadRelacional
    Property EntidadReferida() As IEntidadDN
    ReadOnly Property TipoEntidadReferida() As System.Type

    ReadOnly Property GUIDReferida() As String

    Function VerificarEntidad(ByRef mensaje As String, ByVal pEntidad As IEntidadDN) As Boolean
    Property FechaModificacionEntidadReferida() As Date
    Sub AsignarEntidadReferida(ByVal pEntidad As IEntidadDN)
    Sub AsignarDatosBasicos(ByVal pTipo As System.Type, ByVal pID As String, ByVal pGuid As String)
    Sub Refrescar()

#End Region

End Interface

<Serializable()> _
Public Class HEDN
    Inherits EntidadDN
    Implements IHuellaEntidadDN



#Region "Atributos"


    Protected mGUIDReferida As String
    Protected mIdEntidadReferida As Int64
    Protected mTipoEntidadReferidaFullName As String

    Protected mTipoEntidadReferidaNombreEnsamblado As String

    Protected mToStringEntidadReferida As String
    Protected mIntegridadRelacional As HuellaEntidadDNIntegridadRelacional
    Protected mEntidadReferidaHuella As EntidadDN ' solo se tiene encuenta al grabar pero no al recuperar
    Protected mFechaModificacionEntidadReferida As Date
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pTipo As System.Type, ByVal pID As String, ByVal pGuid As String)
        AsignarDatosBasicos(pTipo, pID, pGuid)
    End Sub


    Public Sub AsignarDatosBasicos(ByVal pTipo As System.Type, ByVal pID As String, ByVal pGuid As String) Implements IHuellaEntidadDN.AsignarDatosBasicos
        mIdEntidadReferida = pID
        mGUIDReferida = pGuid
        AsignarDatosTipo(pTipo)
    End Sub

    Public Sub New(ByVal pentidad As IEntidadDN, ByVal pIntegridadRelacional As HuellaEntidadDNIntegridadRelacional)

        'Dim o As Object
        'o = pentidad
        'mTipoEntidadReferidaFullNme = o.GetType.FullName
        If pentidad Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la entidad referida no puede ser nothing")
        End If
        AsignarEntidadReferida(pentidad)
        Me.modificarEstado = EstadoDatosDN.SinModificar



    End Sub

    Public Sub New(ByVal pentidad As IEntidadDN, ByVal pIntegridadRelacional As HuellaEntidadDNIntegridadRelacional, ByVal pToStringEntidadReferida As String)

        AsignarEntidadReferida(pentidad)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pentidad As IEntidadDN)

        AsignarEntidadReferida(pentidad)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"
    Public Overrides Property ID() As String
        Get

            Return MyBase.ID
        End Get
        Set(ByVal value As String)
            MyBase.ID = value
        End Set
    End Property
    Public Property EstadoModificacion() As EstadoDatosDN
        Get
            Return Me.Estado
        End Get
        Set(ByVal value As EstadoDatosDN)
            Me.modificarEstado = value
        End Set
    End Property

    Public ReadOnly Property IdEntidadReferida() As String Implements IHuellaEntidadDN.IdEntidadReferida
        Get
            Return mIdEntidadReferida
        End Get
    End Property
    Public ReadOnly Property TipoEntidadReferidaFullNme() As String Implements IHuellaEntidadDN.TipoEntidadReferidaFullNme
        Get
            Return mTipoEntidadReferidaFullName
        End Get
    End Property
    Public ReadOnly Property ToStringEntidadReferida() As String Implements IHuellaEntidadDN.ToStringEntidadReferida
        Get
            Return Me.mToStringEntidadReferida
        End Get
    End Property
    Public ReadOnly Property IntegridadRelacional() As HuellaEntidadDNIntegridadRelacional Implements IHuellaEntidadDN.IntegridadRelacional
        Get
            Return mIntegridadRelacional
        End Get
    End Property
    Public Overridable ReadOnly Property TipoEntidadReferida() As System.Type Implements IHuellaEntidadDN.TipoEntidadReferida
        Get
            Dim ensamblado As Reflection.Assembly
            ensamblado = Reflection.Assembly.Load(mTipoEntidadReferidaNombreEnsamblado)
            Return ensamblado.GetType(Me.mTipoEntidadReferidaFullName)
        End Get
    End Property

    Public Property EntidadReferida() As IEntidadDN Implements IHuellaEntidadDN.EntidadReferida
        Get
            Return Me.mEntidadReferidaHuella
        End Get
        Set(ByVal value As IEntidadDN)
            AsignarEntidadReferida(value)
        End Set
    End Property

    Public ReadOnly Property GUIDReferida() As String Implements IHuellaEntidadDN.GUIDReferida
        Get
            Return Me.mGUIDReferida
        End Get
    End Property

    'Private Sub AsignarEntidad(ByVal pEntidadReferida As IEntidadDN)
    '    Dim mensaje As String = ""
    '    If VerificarEntidad(mensaje, pEntidadReferida) Then
    '        Me.CambiarValorRef(Of IEntidadDN)(pEntidadReferida, Me.mEntidadReferidaHuella)
    '        Me.CambiarValorVal(Of String)(pEntidadReferida.ID, Me.mIdEntidadReferida)
    '        Me.CambiarValorVal(Of String)(CType(pEntidadReferida, Object).GetType.FullName, Me.mTipoEntidadReferidaFullNme)
    '        Me.CambiarValorVal(Of String)(CType(pEntidadReferida, Object).GetType.Assembly.FullName, Me.mTipoEntidadReferidaNombreEnsamblado)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    'End Sub

#End Region

    Public Overridable Sub AsignarDatosTipo(ByVal ptipo As System.Type)
        mTipoEntidadReferidaNombreEnsamblado = ptipo.Assembly.GetName.Name
        mTipoEntidadReferidaFullName = ptipo.FullName
    End Sub


    Public Overrides Function AsignarEntidad(ByVal pEntidad As IEntidadBaseDN) As Boolean

        If TypeOf pEntidad Is IEntidadDN Then
            AsignarEntidadReferida(CType(pEntidad, IEntidadDN))
            Return True
        Else
            Return False
        End If

    End Function

    Public Overridable Sub AsignarEntidadReferida(ByVal pEntidad As IEntidadDN) Implements IHuellaEntidadDN.AsignarEntidadReferida



        If TypeOf pEntidad Is IHuellaEntidadDN Then

            Dim h As IHuellaEntidadDN = pEntidad
            AsignarEntidadReferida(h)


        Else




            Dim mensaje As String = ""
            Dim o As Object
            o = pEntidad
            ' mTipoEntidadReferidaFullName = o.GetType.FullName

            AsignarDatosTipo(o.GetType)
            Me.mToSt = o.ToString
            mToStringEntidadReferida = o.ToString

            ' mTipoEntidadReferidaNombreEnsamblado = o.GetType.Assembly.GetName.Name


            ' mID = pEntidad.ID
            If pEntidad.ID Is Nothing OrElse pEntidad.ID = "" Then
                mIdEntidadReferida = 0
            Else
                mIdEntidadReferida = pEntidad.ID

            End If

            Me.mGUID = pEntidad.GUID
            mGUIDReferida = pEntidad.GUID
            Me.CambiarValorPropiedadEntidadRef(pEntidad, mEntidadReferidaHuella)
            'mEntidadReferidaHuella = pEntidad
            mFechaModificacionEntidadReferida = pEntidad.FechaModificacion
            modificarEstado = EstadoDatosDN.Modificado
        End If


    End Sub



    Public Overridable Sub AsignarEntidadReferida(ByVal pEntidadHuella As IHuellaEntidadDN)
        Dim mensaje As String = ""

        AsignarDatosTipo(pEntidadHuella.TipoEntidadReferida)

        mToStringEntidadReferida = pEntidadHuella.ToStringEntidadReferida


        If pEntidadHuella.IdEntidadReferida Is Nothing OrElse pEntidadHuella.IdEntidadReferida = "" Then
            mIdEntidadReferida = 0
        Else
            mIdEntidadReferida = pEntidadHuella.IdEntidadReferida
        End If
        Me.mGUID = pEntidadHuella.GUIDReferida
        mGUIDReferida = pEntidadHuella.GUIDReferida
        mEntidadReferidaHuella = pEntidadHuella.EntidadReferida
        mFechaModificacionEntidadReferida = pEntidadHuella.FechaModificacionEntidadReferida
        modificarEstado = EstadoDatosDN.SinModificar
    End Sub


    Public Overridable Sub Refrescar() Implements IHuellaEntidadDN.Refrescar
        AsignarEntidadReferida(Me.mEntidadReferidaHuella)
        Me.AsignarIDEntidadReferida()
    End Sub
    Public Overridable Sub AsignarIDEntidadReferida()

        If Not mEntidadReferidaHuella Is Nothing Then
            If String.IsNullOrEmpty(mEntidadReferidaHuella.ID) Then

            Else
                mIdEntidadReferida = mEntidadReferidaHuella.ID

            End If

        End If

        mID = mIdEntidadReferida

    End Sub

    Public Overridable Sub EliminarEntidadReferida()
        If Not Me.mEntidadReferidaHuella Is Nothing Then
            Me.CambiarValorPropiedadEntidadRef(Nothing, mEntidadReferidaHuella)
            ' Me.mEntidadReferidaHuella = Nothing
        End If
    End Sub

    'Public Shared Function ValEntidadReferida(ByRef mensaje As String, ByRef pEntidad As IEntidadDN) As Boolean
    '    'If pEntidad.ID Is Nothing OrElse pEntidad.ID = "" Then
    '    '    mensaje = "no es posible establecer una huella contra una entidad sin identificador. Probablemente la entidad todavía no ha sido dada de alta en el sistema"
    '    '    Return False
    '    'Else
    '    '    Return True
    '    'End If
    '    Return True
    'End Function


    Public Function ValEntidadReferida(ByRef mensaje As String, ByVal pEntidad As IEntidadDN) As Boolean Implements IHuellaEntidadDN.VerificarEntidad
        Dim o As Object
        If pEntidad Is Nothing Then
            mensaje = "La entidad referida de la huella no puede ser nothing"
            Return False
        End If
        o = pEntidad
        If o.GetType.FullName = Me.mTipoEntidadReferidaFullName AndAlso pEntidad.ID = Me.mID Then
            Return True
        Else
            mensaje = "La entidad referida de la huella no se corresponde con la entidad a la que se refiere"
            Return False
        End If
    End Function

    Public Property FechaModificacionEntidadReferida() As Date Implements IHuellaEntidadDN.FechaModificacionEntidadReferida
        Get
            Return mFechaModificacionEntidadReferida
        End Get
        Set(ByVal value As Date)
            mFechaModificacionEntidadReferida = value
        End Set
    End Property

    Public Property ConvertirATexto() As String
        Get
            Return Me.TipoEntidadReferidaFullNme & "/" & Me.IdEntidadReferida
        End Get
        Set(ByVal value As String)
            Throw New ApplicationException("not implementes")
        End Set
    End Property


    Public Function GetHuellaTextual() As String

        Dim ht As New HuellaTextual
        ht.GUID = mGUIDReferida
        ht.IdEntidad = mIdEntidadReferida
        ht.tipoEntidadReferida = mTipoEntidadReferidaFullName
        Return ht.GenerarCadena

    End Function


    Protected Overrides Sub OnCambioEstadoDatos()
        MyBase.OnCambioEstadoDatos()
        ' Me.AsignarEntidadReferida(Me.mEntidadReferidaHuella)
    End Sub


End Class

<Serializable()> _
Public Class ColHEDN
    Inherits ArrayListValidable(Of HEDN)

    Public Function AddHuellaPara(ByVal iEntidad As IEntidadDN) As HEDN
        Dim he As New HEDN(iEntidad, HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
        he.EliminarEntidadReferida()
        Me.Add(he)
        Return he

    End Function
    Public Function AddUnicoHuellaPara(ByVal iEntidad As IEntidadDN) As HEDN
        Dim he As New HEDN(iEntidad, HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
        he.EliminarEntidadReferida()
        Me.AddUnico(he)
        Return he

    End Function
End Class

Public Class HuellaTextual

    Public tipoEntidadReferida, IdEntidad, GUID As String
    Public separadores As String() = {"//"}
    Public Function GenerarCadena() As String
        Return Me.tipoEntidadReferida & "//" & Me.IdEntidad & "//" & GUID

    End Function



    Public Sub CargarCadena(ByVal huellaTextual As String)
        Dim parametros() As String
        parametros = huellaTextual.Split(separadores, False)
        tipoEntidadReferida = parametros(0)
        IdEntidad = parametros(1)
        GUID = parametros(2)
    End Sub
End Class



<Serializable()> Public Class ColIHuellaEntidadDN
    Inherits ArrayListValidable(Of IHuellaEntidadDN)

    Public Function EstaEstaHuellaEnLaColeccion(ByVal pHuellaDestinatario As IHuellaEntidadDN) As Boolean
        Dim mHuella As IHuellaEntidadDN
        For Each mHuella In Me
            If mHuella Is pHuellaDestinatario Then
                Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>
    ''' Comprueba que la colección de huellas contiene una huella con el GUID de la entidad referida
    ''' </summary>
    ''' <param name="pGUIDEntidadReferidaDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ContieneGUIDReferida(ByVal pGUIDEntidadReferidaDN As String) As Boolean
        Dim Ent As IHuellaEntidadDN

        For Each Ent In Me
            If Ent.GUIDReferida.ToLower = pGUIDEntidadReferidaDN.ToLower Then
                Return True
            End If
        Next

        Return False

    End Function

End Class