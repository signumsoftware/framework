<Serializable()> _
Public MustInherit Class ElementoMapDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IElemtoMap

    Protected mIco As String
    Protected mNombreVis As String
    Protected mEditable As Boolean = True
    Protected mControlAsignado As String
    Protected mDatosControlAsignado As String = ""
    Protected mDatosBusqueda As String = ""  ' el nombre del mapeado de busqueda por ejemplo
    Protected mInstanciable As Boolean = False
    Protected mAnchoMaximo As Int32 = 0
    Protected mAlto As Integer = -1
    Protected mAncho As Integer = -1

    Protected mBusquedaAutomatica As Boolean = False
    Protected mDevolucionAutomatica As Boolean = False
    Protected mOcultarAccionesxDefecto As Boolean = False
    Protected mFiltrable As Boolean = True
    Protected mFiltroVisible As Boolean = True

    Protected mColEntradaMapNavBuscadorDN As ColEntradaMapNavBuscadorDN
    Protected mColComandoMap As ColComandoMapDN


    Public Sub New()
        Me.CambiarValorRef(Of ColComandoMapDN)(New ColComandoMapDN, mColComandoMap)
        Me.CambiarValorRef(Of ColEntradaMapNavBuscadorDN)(New ColEntradaMapNavBuscadorDN, mColEntradaMapNavBuscadorDN)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub


    Public Property ColComandoMap() As ColComandoMapDN
        Get
            Return Me.mColComandoMap
        End Get
        Set(ByVal value As ColComandoMapDN)
            Me.CambiarValorRef(Of ColComandoMapDN)(value, mColComandoMap)
        End Set
    End Property

    Public Property ColEntradaMapNavBuscadorDN() As ColEntradaMapNavBuscadorDN
        Get
            Return mColEntradaMapNavBuscadorDN
        End Get
        Set(ByVal value As ColEntradaMapNavBuscadorDN)
            mColEntradaMapNavBuscadorDN = value
        End Set
    End Property



    Public Property Alto() As Integer

        Get
            Return mAlto
        End Get

        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mAlto)

        End Set
    End Property








    Public Property Ancho() As Integer

        Get
            Return mAncho
        End Get

        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mAncho)

        End Set
    End Property





    Public Property FiltroVisible() As Boolean
        Get
            Return mFiltroVisible
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mFiltroVisible)

        End Set
    End Property
    Public Property Filtrable() As Boolean
        Get
            Return mFiltrable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mFiltrable)

        End Set
    End Property

    Public Property DevolucionAutomatica() As Boolean
        Get
            Return Me.mDevolucionAutomatica
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mDevolucionAutomatica)

        End Set
    End Property
    Public Property BusquedaAutomatica() As Boolean
        Get
            Return Me.mBusquedaAutomatica
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mBusquedaAutomatica)

        End Set
    End Property


    Public Property AnchoMaximo() As Int32
        Get
            Return Me.mAnchoMaximo
        End Get
        Set(ByVal value As Int32)
            Me.CambiarValorVal(Of Int32)(value, Me.mAnchoMaximo)
        End Set
    End Property

    Public Property Instanciable() As Boolean
        Get
            Return Me.mInstanciable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mInstanciable)

        End Set
    End Property

    Public Property DatosBusqueda() As String
        Get
            Return Me.mDatosBusqueda

        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mDatosBusqueda)
        End Set
    End Property
    Public Property DatosControlAsignado() As String
        Get
            Return Me.mDatosControlAsignado

        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mDatosControlAsignado)
        End Set
    End Property
    Public Property ControlAsignado() As String
        Get
            Return Me.mControlAsignado

        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mControlAsignado)
        End Set
    End Property

    Public Property Editable() As Boolean Implements IElemtoMap.Editable
        Get
            Return mEditable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mEditable)

        End Set
    End Property
    Public Property NombreVis() As String Implements IElemtoMap.NombreVis
        Get
            Return Me.mNombreVis

        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreVis)
        End Set
    End Property


    Public Property Ico() As String Implements IElemtoMap.Ico
        Get
            Return Me.mIco

        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mIco)
        End Set
    End Property

    Public Property OcultarAccionesxDefecto() As Boolean
        Get
            Return mOcultarAccionesxDefecto
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mOcultarAccionesxDefecto)
        End Set
    End Property

    Public Overrides Function ToXML() As String

        Return ToXML(GetType(ElementoMapXML))

    End Function


    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        Return FromXML(GetType(ElementoMapXML), ptr)
    End Function

    Public MustOverride Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap




End Class


Public Class ElementoMapXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    <Xml.Serialization.XmlAttribute()> Public Instanciable As Boolean
    <Xml.Serialization.XmlAttribute()> Public Ico As String
    <Xml.Serialization.XmlAttribute()> Public NombreVis As String
    <Xml.Serialization.XmlAttribute()> Public Editable As Boolean
    <Xml.Serialization.XmlAttribute()> Public ControlAsignado As String
    <Xml.Serialization.XmlAttribute()> Public DatosControlAsignado As String = ""
    <Xml.Serialization.XmlAttribute()> Public DatosBusqueda As String = ""
    <Xml.Serialization.XmlAttribute()> Public AnchoMaximo As Int32 = 0
    <Xml.Serialization.XmlAttribute()> Public DevolucionAutomatica As Boolean
    <Xml.Serialization.XmlAttribute()> Public BusquedaAutomatica As Boolean
    <Xml.Serialization.XmlAttribute()> Public OcultarAccionesxDefecto As Boolean
    <Xml.Serialization.XmlAttribute()> Public Filtrable As Boolean
    <Xml.Serialization.XmlAttribute()> Public FiltroVisible As Boolean
    <Xml.Serialization.XmlAttribute()> Public Ancho As Int32 = -1
    <Xml.Serialization.XmlAttribute()> Public Alto As Int32 = -1
    Public ColEntradaMapNavBuscador As New List(Of EntradaMapNavBuscadorXML)
    Public ColComandoMap As New List(Of ComandoMapDNXml)

    Public Overridable Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As ElementoMapDN
        entidad = pEntidad
        Instanciable = entidad.Instanciable

        Ico = entidad.Ico
        NombreVis = entidad.NombreVis
        Editable = entidad.Editable
        ControlAsignado = entidad.ControlAsignado
        DatosControlAsignado = entidad.DatosControlAsignado
        DatosBusqueda = entidad.DatosBusqueda
        AnchoMaximo = entidad.AnchoMaximo
        DevolucionAutomatica = entidad.DevolucionAutomatica
        BusquedaAutomatica = entidad.BusquedaAutomatica
        OcultarAccionesxDefecto = entidad.OcultarAccionesxDefecto
        Filtrable = entidad.Filtrable
        FiltroVisible = entidad.FiltroVisible
        Alto = entidad.Alto
        Ancho = entidad.Ancho

        entidad.ColEntradaMapNavBuscadorDN.ToListIXMLAdaptador(New EntradaMapNavBuscadorXML, ColEntradaMapNavBuscador)
        entidad.ColComandoMap.ToListIXMLAdaptador(New ComandoMapDNXml, ColComandoMap)

    End Sub

    Public Overridable Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As ElementoMapDN
        entidad = pEntidad
        entidad.Instanciable = Instanciable

        entidad.Ico = Ico
        entidad.NombreVis = Me.NombreVis
        entidad.Editable = Me.Editable
        entidad.ControlAsignado = ControlAsignado
        entidad.DatosControlAsignado = DatosControlAsignado
        entidad.DatosBusqueda = DatosBusqueda
        entidad.AnchoMaximo = AnchoMaximo
        entidad.DevolucionAutomatica = DevolucionAutomatica
        entidad.BusquedaAutomatica = BusquedaAutomatica
        entidad.OcultarAccionesxDefecto = OcultarAccionesxDefecto
        entidad.Filtrable = Filtrable
        entidad.FiltroVisible = FiltroVisible
        entidad.Alto = Alto
        entidad.Ancho = Ancho

        For Each miVinculoClaseMapXml As EntradaMapNavBuscadorXML In ColEntradaMapNavBuscador
            Dim ag As New EntradaMapNavBuscadorDN
            miVinculoClaseMapXml.XMLAdaptadorToObjeto(ag)
            entidad.ColEntradaMapNavBuscadorDN.Add(ag)
        Next

        For Each opxml As ComandoMapDNXml In ColComandoMap
            Dim cm As New ComandoMapDN
            opxml.XMLAdaptadorToObjeto(cm)
            entidad.ColComandoMap.Add(cm)
        Next

    End Sub
End Class