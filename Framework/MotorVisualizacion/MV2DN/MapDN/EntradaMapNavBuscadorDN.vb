
Public Class EntradaMapNavBuscadorDN
    Inherits ElementoMapDN

    Public micono As String
    Public mNombreEnsambladoyTipo As String
    'Public mNombreVista As String
    Public mNombreCampo As String
    Public mNombrePropiedad As String = "ID"
    Protected mEsNavegable As Boolean = True
    Protected mMostrarComandosDelTipo As Boolean










    ''' <summary>
    ''' en un navegador si esta activada peromite que los comandos que se pueden ejecutar sobre un tipo se puedan ejecutar desde el navegador
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property MostrarComandosDelTipo() As Boolean

        Get
            Return mMostrarComandosDelTipo
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mMostrarComandosDelTipo)

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

    Public Property NombrePropiedad() As String
        Get
            Return mNombrePropiedad
        End Get
        Set(ByVal value As String)
            mNombrePropiedad = value
        End Set
    End Property
    Public Property NombreEnsambladoyTipo() As String
        Get
            Return mNombreEnsambladoyTipo
        End Get
        Set(ByVal value As String)
            mNombreEnsambladoyTipo = value
        End Set
    End Property

    Public Property NombreCampo() As String
        Get
            Return mNombreCampo
        End Get
        Set(ByVal value As String)
            mNombreCampo = value
        End Set
    End Property

    'Public Property NombreVista() As String
    '    Get
    '        Return mNombreVista
    '    End Get
    '    Set(ByVal value As String)
    '        mNombreVista = value
    '    End Set
    'End Property

    Public Property icono() As String
        Get
            Return micono
        End Get
        Set(ByVal value As String)
            micono = value
        End Set
    End Property



    Public Function RecuperarValor(ByVal pentidad As Framework.DatosNegocio.IEntidadDN) As String


        Dim o As Object = pentidad

        Dim pv As System.Reflection.PropertyInfo = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.obtenerLaPropiedad(o.GetType(), mNombrePropiedad)


        Return pv.GetValue(pentidad, Nothing)


    End Function



    Public Property Tipo() As System.Type
        Get
            If String.IsNullOrEmpty(mNombreEnsambladoyTipo) Then
                Return Nothing
            End If
            Dim ptipo As System.Type
            Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(mNombreEnsambladoyTipo, Nothing, ptipo)
            Return ptipo
        End Get
        Set(ByVal value As System.Type)
            mNombreEnsambladoyTipo = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.TipoToString(value)
        End Set
    End Property


    Public Overrides Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap

    End Function
End Class


Public Class ColEntradaMapNavBuscadorDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EntradaMapNavBuscadorDN)

End Class


Public Class EntradaMapNavBuscadorXML
    Inherits ElementoMapXML

    <Xml.Serialization.XmlAttribute()> Public icono As String
    <Xml.Serialization.XmlAttribute()> Public NombreEnsambladoyTipo As String
    '  <Xml.Serialization.XmlAttribute()> Public NombreVista As String
    <Xml.Serialization.XmlAttribute()> Public NombreCampo As String
    <Xml.Serialization.XmlAttribute()> Public NombrePropiedad As String
    <Xml.Serialization.XmlAttribute()> Public EsNavegable As Boolean
    <Xml.Serialization.XmlAttribute()> Public MostrarComandosDelTipo As Boolean

    Public Overrides Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        Dim entidad As EntradaMapNavBuscadorDN = pEntidad
        icono = entidad.icono
        NombreEnsambladoyTipo = entidad.NombreEnsambladoyTipo
        '  NombreVista = entidad.NombreVista
        NombreCampo = entidad.NombreCampo
        NombrePropiedad = entidad.mNombrePropiedad
        EsNavegable = entidad.EsNavegable
        MostrarComandosDelTipo = entidad.MostrarComandosDelTipo

        MyBase.ObjetoToXMLAdaptador(pEntidad)

    End Sub



    Public Overrides Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        MyBase.XMLAdaptadorToObjeto(pEntidad)



        Dim entidad As EntradaMapNavBuscadorDN = pEntidad
        entidad.icono = icono
        entidad.NombreEnsambladoyTipo = NombreEnsambladoyTipo
        '  entidad.NombreVista = NombreVista
        entidad.NombreCampo = NombreCampo
        entidad.mNombrePropiedad = NombrePropiedad
        entidad.EsNavegable = EsNavegable
        entidad.MostrarComandosDelTipo = MostrarComandosDelTipo


    End Sub
End Class

