Imports System.Xml
Imports Framework.TiposYReflexion.DN

<Serializable()> _
Public Class InstanciaMapDN
    Inherits ElementoMapDN
    Implements IElemtoMapContenedor




#Region "Atributos"

    Protected mColAgrupacionMap As ColAgrupacionMapDN
    '  Protected mColComandoMap As ColComandoMapDN
    Protected mColNombresTiposComaptibles As ColVinculoClaseDN

    Protected mArbolNavegacionVisible As Boolean
    Protected mNavegacionesAutomaticas As Boolean
    ' Protected mColOperaciones As Framework.Procesos.ProcesosDN.ColOperacionDN


#End Region

#Region "Constructores"

    Public Sub New()


        ' Me.CambiarValorRef(Of ColComandoMapDN)(New ColComandoMapDN, mColComandoMap)
        Me.CambiarValorRef(Of ColVinculoClaseDN)(New ColVinculoClaseDN, mColNombresTiposComaptibles)

        'Me.CambiarValorRef(Of Framework.Procesos.ProcesosDN.ColOperacionDN)(New Framework.Procesos.ProcesosDN.ColOperacionDN, mColOperaciones)
        Me.CambiarValorRef(Of ColAgrupacionMapDN)(New ColAgrupacionMapDN, mColAgrupacionMap)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

 
    Public Property ColNombresTiposComaptibles() As ColVinculoClaseDN
        Get
            Return mColNombresTiposComaptibles
        End Get
        Set(ByVal value As ColVinculoClaseDN)
            Me.CambiarValorRef(Of ColVinculoClaseDN)(value, mColNombresTiposComaptibles)
        End Set
    End Property

    Public Property ColAgrupacionMap() As ColAgrupacionMapDN
        Get
            Return mColAgrupacionMap
        End Get
        Set(ByVal value As ColAgrupacionMapDN)
            Me.CambiarValorRef(Of ColAgrupacionMapDN)(value, mColAgrupacionMap)

        End Set
    End Property

    'Public Property ColComandoMap() As ColComandoMapDN
    '    Get
    '        Return Me.mColComandoMap
    '    End Get
    '    Set(ByVal value As ColComandoMapDN)
    '        Me.CambiarValorRef(Of ColComandoMapDN)(value, mColComandoMap)
    '    End Set
    'End Property

    Public Property ArbolNavegacionVisible() As Boolean
        Get
            Return mArbolNavegacionVisible
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mArbolNavegacionVisible)
        End Set
    End Property

    Public Property NavegacionesAutomaticas() As Boolean
        Get
            Return mNavegacionesAutomaticas
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mNavegacionesAutomaticas)
        End Set
    End Property

    'Public Property ColOperaciones() As Framework.Procesos.ProcesosDN.ColOperacionDN
    '    Get
    '        Return mColOperaciones
    '    End Get
    '    Set(ByVal value As Framework.Procesos.ProcesosDN.ColOperacionDN)
    '        CambiarValorCol(Of Framework.Procesos.ProcesosDN.ColOperacionDN)(value, mColOperaciones)
    '    End Set
    'End Property

#End Region

#Region "Métodos"

    Public Overrides Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap
        ' devuelve el elemento contenedor

        If TypeOf pElementoMap Is AgrupacionMapDN Then
            If Me.mColAgrupacionMap.Contiene(pElementoMap, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                Return Me
            End If
        End If

        For Each map As AgrupacionMapDN In Me.mColAgrupacionMap
            Dim micontenedor As ElementoMapDN
            'If map Is pElementoMap Then
            '    Return Me
            'End If

            micontenedor = map.ElementoContenedor(pElementoMap)
            If Not micontenedor Is Nothing Then
                Return micontenedor
            End If

        Next

        Return Nothing

    End Function

    Public Sub MoverNElementoMap(ByVal pN As Integer, ByVal pElementoMap As ElementoMapDN)
        Dim ag As AgrupacionMapDN = ElementoContenedor(pElementoMap)

        ag.MoverNElementoMap(pN, pElementoMap)
    End Sub

    Public Function EliminarElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean Implements IElemtoMapContenedor.EliminarElementoMap
        Dim elm As IElemtoMap = ElementoContenedor(pElementoMap)

        If elm Is Me Then
            Dim ag As AgrupacionMapDN = pElementoMap
            Me.mColAgrupacionMap.EliminarEntidadDN(ag, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        End If


        If TypeOf elm Is InstanciaMapDN Then
            Dim im As InstanciaMapDN = elm

            If im IsNot Nothing Then
                im.EliminarElementoMap(pElementoMap)

            End If

        Else
            Dim ag As AgrupacionMapDN = elm
            If ag IsNot Nothing Then
                ag.EliminarElementoMap(pElementoMap)

            End If

        End If

        Return Nothing

    End Function

    Public Shared Function CrearInstanciaMapDNBaseDN(ByVal pTipo As System.Type) As InstanciaMapDN

        Dim nombre As String = "BASICA-ENT"
        Dim imap As New MV2DN.InstanciaMapDN
        Dim tipoag As New TipoAgrupacionMapDN
        tipoag.ID = 1
        tipoag.Nombre = nombre

        Dim agmap As New MV2DN.AgrupacionMapDN(imap, tipoag)
        '        imap.Nombre = pTipo.Name & "-BASICA"
        Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(pTipo)
        imap.Nombre = vc.NombreClase & "-" & nombre

        Dim pi As Reflection.PropertyInfo
        Dim pimap As MV2DN.PropMapDN

        For Each pi In pTipo.GetProperties
            If pi.Name = "GUID" Or pi.Name = "EstadoModificacion" Or pi.Name = "FechaModificacion" Or pi.Name = "Estado" Or pi.Name = "CampoUsuario" Or pi.Name = "HashValores" Or pi.Name = "ColCampoUsuario" Or pi.Name = "Baja" Or pi.Name = "FI" Or pi.Name = "FF" Then

                ' ignorar

            Else
                If pi.CanRead Then
                    pimap = New MV2DN.PropMapDN
                    pimap.NombreProp = pi.Name
                    pimap.NombreVis = pi.Name
                    pimap.Editable = pi.CanWrite
                    agmap.ColPropMap.Add(pimap)
                End If
            End If

        Next

        agmap.ColPropMap.sort(New OrdenadorMapeadoBaseDN)

        Return imap

    End Function

    Public Shared Function CrearInstanciaMapDNBase(ByVal pTipo As System.Type) As InstanciaMapDN

        Dim nombre As String = "BASICA-ENT"

        Dim imap As New MV2DN.InstanciaMapDN
        Dim tipoag As New TipoAgrupacionMapDN
        tipoag.ID = 1
        tipoag.Nombre = nombre

        Dim agmap As New MV2DN.AgrupacionMapDN(imap, tipoag)
        'imap.Nombre = pTipo.Name & "-BASICA"
        Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(pTipo)
        imap.Nombre = vc.NombreClase & "-" & nombre


        Dim pi As Reflection.PropertyInfo
        Dim pimap As MV2DN.PropMapDN

        For Each pi In pTipo.GetProperties
            If pi.CanRead Then
                pimap = New MV2DN.PropMapDN
                pimap.NombreProp = pi.Name
                pimap.NombreVis = pi.Name
                pimap.Editable = pi.CanWrite
                agmap.ColPropMap.Add(pimap)
            End If
        Next

        Return imap

    End Function

    Public Overrides Function ToXML() As String

        'Dim stb As New System.Text.StringBuilder

        'stb.AppendLine("<InstanciaMapDN " & MyBase.ToXML() & ">")
        'stb.AppendLine(mColAgrupacionMap.ToXML)
        'stb.AppendLine("</InstanciaMapDN>")

        'Return stb.ToString

        Return ToXML(GetType(InstanciaMapXml))

    End Function

    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        Return FromXML(GetType(InstanciaMapXml), ptr)
    End Function

    Public Overrides Sub ElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
        MyBase.ElementoAñadido(pSender, pElemento)
        If pSender Is Me.mColAgrupacionMap Then
            Dim ag As AgrupacionMapDN = pElemento
            ag.InstanciaContenedora = Me
        End If

    End Sub

#End Region

    Public Property ColIElemtoMap() As System.Collections.Generic.List(Of IElemtoMap) Implements IElemtoMapContenedor.ColIElemtoMap
        Get
            Return Nothing
        End Get
        Set(ByVal value As System.Collections.Generic.List(Of IElemtoMap))

        End Set
    End Property

    Public Property ColIElemtoMapContenedor() As System.Collections.Generic.List(Of IElemtoMapContenedor) Implements IElemtoMapContenedor.ColIElemtoMapContenedor
        Get
            Return New System.Collections.Generic.List(Of IElemtoMapContenedor)(Me.mColAgrupacionMap.ToArray)

        End Get
        Set(ByVal value As System.Collections.Generic.List(Of IElemtoMapContenedor))

        End Set
    End Property

    Public Property InstanciaContenedora() As IElemtoMap Implements IElemtoMapContenedor.InstanciaContenedora
        Get
            Return Me
        End Get
        Set(ByVal value As IElemtoMap)

        End Set
    End Property

    Public Function AñadirElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean Implements IElemtoMapContenedor.AñadirElementoMap
        If TypeOf pElementoMap Is AgrupacionMapDN Then
            Me.mColAgrupacionMap.Add(pElementoMap)
        Else
            Me.mColAgrupacionMap(0).AñadirElementoMap(pElementoMap)
        End If



    End Function
    Public Function PosicionElementoMap(ByVal pIElemtoMap As IElemtoMap) As Integer





        Return Me.mColAgrupacionMap.IndexOf(pIElemtoMap)





    End Function
    Public Function AñadirElementoMapEnRelacion(ByVal pElementoMap As IElemtoMap, ByVal pElementoMapRelacioando As IElemtoMap, ByVal pPosicion As Posicion) As Boolean Implements IElemtoMapContenedor.AñadirElementoMapEnRelacion
        Dim posi As Integer = PosicionElementoMap(pElementoMapRelacioando)
        If pPosicion = Posicion.Despues Then
            posi += 1
        End If


        If TypeOf pElementoMap Is AgrupacionMapDN Then
            Me.mColAgrupacionMap.Insert(posi, pElementoMap)
        End If




    End Function
End Class


Public Class ColInstanciaMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of InstanciaMapDN)

End Class



Public Class InstanciaMapXml
    Inherits ElementoMapXML

    <Xml.Serialization.XmlAttribute()> Public Nombre As String
    Public ColAgrupacionMap As New List(Of AgrupacionMapXML)
    ' Public ColComandoMap As New List(Of ComandoMapDNXml)
    Public ColNombresTiposComaptibles As New List(Of VinculoClaseMapXml)
    '  Public ColEntradaMapNavBuscador As New List(Of EntradaMapNavBuscadorXML)
    <Xml.Serialization.XmlAttribute()> Public ArbolNavegacionVisible As Boolean
    <Xml.Serialization.XmlAttribute()> Public NavegacionesAutomaticas As Boolean
    'Public ColOperaciones As New List(Of Framework.Procesos.ProcesosDN.OperacionDNMapXml)


    Public Overrides Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        Dim entidad As InstanciaMapDN = pEntidad
        Nombre = entidad.Nombre
        ArbolNavegacionVisible = entidad.ArbolNavegacionVisible
        NavegacionesAutomaticas = entidad.NavegacionesAutomaticas

        MyBase.ObjetoToXMLAdaptador(pEntidad)
        entidad.ColAgrupacionMap.ToListIXMLAdaptador(New AgrupacionMapXML, ColAgrupacionMap)
        ' entidad.ColOperaciones.ToListIXMLAdaptador(New Framework.Procesos.ProcesosDN.OperacionDNMapXml, ColOperaciones)
        '     entidad.ColComandoMap.ToListIXMLAdaptador(New ComandoMapDNXml, ColComandoMap)

        entidad.ColNombresTiposComaptibles.ToListIXMLAdaptador(New VinculoClaseMapXml, ColNombresTiposComaptibles)



    End Sub

    Public Overrides Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)


        MyBase.XMLAdaptadorToObjeto(pEntidad)

        Dim entidad As InstanciaMapDN = pEntidad
        entidad.Nombre = Me.Nombre
        entidad.ArbolNavegacionVisible = Me.ArbolNavegacionVisible
        entidad.NavegacionesAutomaticas = Me.NavegacionesAutomaticas

        For Each AGxml As AgrupacionMapXML In ColAgrupacionMap
            Dim ag As New AgrupacionMapDN
            AGxml.XMLAdaptadorToObjeto(ag)
            entidad.ColAgrupacionMap.Add(ag)
        Next


        'For Each opxml As ComandoMapDNXml In ColComandoMap
        '    Dim cm As New ComandoMapDN
        '    opxml.XMLAdaptadorToObjeto(cm)
        '    entidad.ColComandoMap.Add(cm)
        'Next

        'For Each opxml As Framework.Procesos.ProcesosDN.OperacionDNMapXml In ColOperaciones
        '    Dim op As New Framework.Procesos.ProcesosDN.OperacionDN
        '    opxml.XMLAdaptadorToObjeto(op)
        '    entidad.ColOperaciones.Add(op)
        'Next



        For Each miVinculoClaseMapXml As VinculoClaseMapXml In ColNombresTiposComaptibles
            Dim ag As New VinculoClaseDN
            miVinculoClaseMapXml.XMLAdaptadorToObjeto(ag)
            entidad.ColNombresTiposComaptibles.Add(ag)
        Next



    End Sub
End Class


Public Class OrdenadorMapeadoBaseDN
    Implements IComparer(Of PropMapDN)



    Public Function Compare(ByVal x As PropMapDN, ByVal y As PropMapDN) As Integer Implements System.Collections.Generic.IComparer(Of PropMapDN).Compare
        Dim dx As Int16 = RecuperarValor(x.NombreProp)
        Dim dy As Int16 = RecuperarValor(y.NombreProp)
        Return dx.CompareTo(dy)


    End Function


    Private Function RecuperarValor(ByVal cadena As String) As Int16
        Dim cadenas As New List(Of String)
        cadenas.AddRange([Enum].GetNames(GetType(OrdenPropiedades)))

        If cadenas.Contains(cadena) Then
            Return [Enum].Parse(GetType(OrdenPropiedades), cadena, True)
        End If
        Return OrdenPropiedades.Desconocida
    End Function

    Private Enum OrdenPropiedades
        ID
        Nombre
        Periodo
        Desconocida
    End Enum
End Class