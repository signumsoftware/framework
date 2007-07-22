Imports Framework.TiposYReflexion.DN

<Serializable()> _
Public Class PropMapDN

    Inherits ElementoMapDN

    Protected mInstanciaContenedora As InstanciaMapDN
    Protected mNombreProp As String
    Protected mCargarDatos As Boolean = False
    Protected mEsReadOnly As Boolean = False
    Protected mEsEliminable As Boolean = True
    Protected mEsBuscable As Boolean = True
    Protected mEsNavegable As Boolean = True
    Protected mDatosNavegacion As String
    Protected mVisibleCabeceraResumen As Boolean = False
    Protected mInvisibleSiNothing As Boolean = False

    Protected mMedidasIcoLabText As String = "-1*f/-1*f/-1*f/-1*f"


    Protected mColNombresTiposComaptibles As ColVinculoClaseDN
    ''' <summary>
    ''' este campo es usado cuando una isntacia que es referida por una propiedad de un tipo quiere ser submapeada por otro tipo distiento que debe implementar
    ''' el formato es "nombreEnsamblado/nameespace.nameespace...nombreclase"
    ''' </summary>
    ''' <remarks></remarks>
    Protected mTipoImpuesto As String

    ''' <summary>
    ''' indica que es una propidad que no se encontrará en el tipo , in que puede servir para introducir una label o un control de buscador
    ''' 
    ''' el tipo de el control a enlazar vendran determinados en la propiedad datos control
    ''' </summary>
    ''' <remarks></remarks>
    Protected mVirtual As Boolean
    Public Sub New()
        Me.CambiarValorRef(Of ColVinculoClaseDN)(New ColVinculoClaseDN, mColNombresTiposComaptibles)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub







    Public Property Virtual() As Boolean

        Get
            Return mVirtual
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mVirtual)

        End Set
    End Property





    Public Property MedidasIcoLabText() As String
        Get
            Return mMedidasIcoLabText
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mMedidasIcoLabText)
        End Set
    End Property



    Public Property TipoImpuesto() As String

        Get
            Return mTipoImpuesto
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mTipoImpuesto)

        End Set
    End Property





    Public Property InvisibleSiNothing() As Boolean
        Get
            Return mInvisibleSiNothing
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mInvisibleSiNothing)

        End Set
    End Property

    Public Property ColNombresTiposComaptibles() As ColVinculoClaseDN
        Get
            Return mColNombresTiposComaptibles
        End Get
        Set(ByVal value As ColVinculoClaseDN)
            Me.CambiarValorRef(Of ColVinculoClaseDN)(value, mColNombresTiposComaptibles)
        End Set
    End Property

    Public Property DatosNavegacion() As String
        Get
            Return mDatosNavegacion
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mDatosNavegacion)
        End Set
    End Property

    Public Property EsNavegable() As Boolean
        Get
            Return Me.mEsNavegable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mEsNavegable)

        End Set
    End Property

    Public Property EsBuscable() As Boolean
        Get
            Return Me.mEsBuscable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mEsBuscable)

        End Set
    End Property

    Public Property EsEliminable() As Boolean
        Get
            Return Me.mEsEliminable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mEsEliminable)

        End Set
    End Property

    Public Property EsReadOnly() As Boolean
        Get
            Return Me.mEsReadOnly
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mEsReadOnly)

        End Set
    End Property

    Public ReadOnly Property EsPropiedadEncadenada() As Boolean
        Get
            Return mNombreProp.Contains(".")
        End Get
    End Property

    Public Property VisibleCabeceraResumen() As Boolean
        Get
            Return mVisibleCabeceraResumen
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mVisibleCabeceraResumen)
        End Set
    End Property


    Public Overrides Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap
        Return Nothing
    End Function

    Public Property CargarDatos() As Boolean
        Get
            Return Me.mCargarDatos
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mCargarDatos)
            'If value Then
            '    Me.Instanciable = False
            '    Me.EsBuscable = False
            '    Me.Editable = False
            'End If

        End Set
    End Property
    Public Property NombreProp() As String
        Get
            Return mNombreProp
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mNombreProp)
        End Set
    End Property
    Public ReadOnly Property NombrePropSinPrimeraEntidad() As String
        Get
            Dim pospuesto As Integer = mNombreProp.IndexOf(".")
            If pospuesto > 0 Then
                Return mNombreProp.Substring(pospuesto + 1)


            Else
                Return mNombreProp

            End If



        End Get

    End Property
    Public Overrides Function ToXML() As String
        Return Me.ToXML(GetType(PropMapXML))
    End Function
    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        ' Return FromXML(GetType(AgrupacionMapXML), ptr)
        Return FromXML(GetType(PropMapXML), ptr)
    End Function


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        If Me.mVirtual AndAlso Me.ColEntradaMapNavBuscadorDN.Count < 1 Then
            pMensaje = "una p`ropiedad viertual requiere almenos una entrada en la ColEntradaMapNavBuscadorDN"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


End Class


Public Class ColPropMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of PropMapDN)

End Class


Public Class PropMapXML
    Inherits ElementoMapXML
    <Xml.Serialization.XmlAttribute()> Public NombreProp As String
    <Xml.Serialization.XmlAttribute()> Public CargarDatos As String
    <Xml.Serialization.XmlAttribute()> Public VisibleCabeceraResumen As Boolean
    <Xml.Serialization.XmlAttribute()> Public EsReadOnly As Boolean
    <Xml.Serialization.XmlAttribute()> Public EsEliminable As Boolean
    <Xml.Serialization.XmlAttribute()> Public EsBuscable As Boolean
    <Xml.Serialization.XmlAttribute()> Public EsNavegable As Boolean
    <Xml.Serialization.XmlAttribute()> Public InvisibleSiNothing As Boolean
    <Xml.Serialization.XmlAttribute()> Public DatosNavegacion As String
    <Xml.Serialization.XmlAttribute()> Public TipoImpuesto As String
    <Xml.Serialization.XmlAttribute()> Public MedidasIcoLabText As String
    <Xml.Serialization.XmlAttribute()> Public Virtual As Boolean = False


    Public ColNombresTiposComaptibles As New List(Of VinculoClaseMapXml)

    Public Overrides Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)


        Dim entidad As PropMapDN = pEntidad
        MedidasIcoLabText = entidad.MedidasIcoLabText
        NombreProp = entidad.NombreProp
        Virtual = entidad.Virtual
        CargarDatos = entidad.CargarDatos
        VisibleCabeceraResumen = entidad.VisibleCabeceraResumen
        EsReadOnly = entidad.EsReadOnly
        EsEliminable = entidad.EsEliminable
        EsBuscable = entidad.EsBuscable
        EsNavegable = entidad.EsNavegable
        DatosNavegacion = entidad.DatosNavegacion
        InvisibleSiNothing = entidad.InvisibleSiNothing
        TipoImpuesto = entidad.TipoImpuesto
        '  NombresTiposComaptibles.AddRange(entidad.NombresTiposComaptibles.ToArray)
        entidad.ColNombresTiposComaptibles.ToListIXMLAdaptador(New VinculoClaseMapXml, ColNombresTiposComaptibles)

        MyBase.ObjetoToXMLAdaptador(pEntidad)

    End Sub

    Public Overrides Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        MyBase.XMLAdaptadorToObjeto(pEntidad)
        Dim entidad As PropMapDN = pEntidad

        entidad.CargarDatos = CargarDatos
        entidad.MedidasIcoLabText = MedidasIcoLabText
        entidad.Virtual = Virtual

        entidad.NombreProp = NombreProp
        entidad.VisibleCabeceraResumen = VisibleCabeceraResumen
        entidad.EsReadOnly = EsReadOnly
        entidad.EsEliminable = EsEliminable
        entidad.EsBuscable = EsBuscable
        entidad.EsNavegable = EsNavegable
        entidad.InvisibleSiNothing = InvisibleSiNothing
        entidad.TipoImpuesto = TipoImpuesto
        entidad.DatosNavegacion = DatosNavegacion
        'Dim col As New ColVinculoClaseDN
        'col.AddRange(NombresTiposComaptibles.ToArray)
        'entidad.NombresTiposComaptibles = col


        For Each miVinculoClaseMapXml As VinculoClaseMapXml In ColNombresTiposComaptibles
            Dim ag As New VinculoClaseDN
            miVinculoClaseMapXml.XMLAdaptadorToObjeto(ag)
            entidad.ColNombresTiposComaptibles.Add(ag)
        Next




    End Sub
End Class

