''' <summary>Esta clase representa la base para una entidad de datos de negocio sin persistencia.</summary>

<Serializable()> _
Public MustInherit Class EntidadBaseDN
    Implements IEntidadBaseDN


#Region "Atributos"
    'El id del objeto
    Protected mID As String

    'El nombre del objeto
    Protected mNombre As String

    'El id del objeto para la serializacion
    'Protected mIDS As String
    Protected mGUID As String

    'Estado de modificacion del objeto
    Protected Friend mEstado As EstadoDatosDN

    ' debe de cahcear en modo testo la informacion para que los buscadores puedan ofrecer una visión resumida de la entidad
    Protected mToSt As String

#End Region

#Region "Constructores"
    ''' <overloads>El constructor esta sobrecargado.</overloads>
    ''' <summary>Constructor por defecto.</summary>
    ''' <remarks>Por defecto el id y el nombre son la cadena vacia.</remarks>
    Public Sub New()
        mID = String.Empty
        mNombre = String.Empty
        mGUID = Me.GenerarIDGlobal()
    End Sub

    ''' <summary>
    ''' Constructor que acepta un id y un nombre.
    ''' </summary>
    ''' <param name="pId" type="String">
    ''' Id que vamos a asignar al objeto.
    ''' </param>
    ''' <param name="pNombre" type="String">
    ''' Nombre que vamos a asignar al objeto.
    ''' </param>
    Public Sub New(ByVal pId As String, ByVal pNombre As String)
        If (pId Is Nothing) Then
            mID = String.Empty

        Else
            mID = pId
        End If

        If (pNombre Is Nothing) Then
            mNombre = String.Empty

        Else
            mNombre = pNombre
        End If

        mGUID = Me.GenerarIDGlobal()
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o modifica el id de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> Public Overridable Property ID() As String Implements IEntidadBaseDN.ID
        Get
            Return mID
        End Get
        Set(ByVal Value As String)
            If Value Is Nothing Then
                mID = String.Empty

            Else
                mID = Value
            End If
        End Set
    End Property

    ''' <summary>Obtiene o modifica el nombre de la entidad.</summary>
    Public Overridable Property Nombre() As String Implements IEntidadBaseDN.Nombre
        Get
            Return mNombre
        End Get
        Set(ByVal Value As String)
            If Value Is Nothing Then
                mNombre = String.Empty

            Else
                If Value <> mNombre Then
                    mNombre = Value
                    Me.mEstado = EstadoDatosDN.Modificado

                End If

            End If
        End Set
    End Property

    Private Function ClaveEntidad() As String Implements IEntidadBaseDN.ClaveEntidad
        Return ClaveEntidad(Me.GetType, mID)
    End Function

    Public Shared Function ClaveEntidad(ByVal pTipo As System.Type, ByVal pID As String) As String
        Return pTipo.FullName & "--ID:" & pID
    End Function

    Public ReadOnly Property GUID() As String Implements IEntidadBaseDN.GUID
        Get
            Return Me.mGUID
        End Get
    End Property

    Public ReadOnly Property ToStringEntidad() As String
        Get
            Return Me.ToString()
        End Get
    End Property

    ' '' solo hace una comparacion ligera del tipo y el ide 
    'Public Overrides Function Equals(ByVal o As Object) As Boolean
    '    If Me.GetType() Is o.GetType() Then
    '        Return CType(o, EntidadBaseDN).ID = Me.ID
    '    End If
    '    Return False
    'End Function

    'Public Overrides Function GetHashCode() As Integer
    '    Return ClaveEntidad().GetHashCode()
    'End Function

    'Public Function EqualsEnProfundidad(ByVal o As Object) As Boolean
    '    Return EqualsEnProfundidadInmersion(o, New ArrayList())

    'End Function

    'Private Function EqualsEnProfundidadInmersion(ByVal o As Object, ByVal visitados As ArrayList) As Boolean

    'End Function

#End Region

#Region "Propiedades IEntidadBaseDN.Estado"
    Public ReadOnly Property Estado() As EstadoDatosDN Implements IEntidadBaseDN.Estado
        Get

        End Get
    End Property

    Private Function ToString1() As String Implements IEntidadBaseDN.ToString
        Return Me.ToString()
    End Function

#End Region

#Region "Métodos"
    Public Overridable Function FromXML(ByVal ptr As IO.TextReader) As Object Implements IEntidadBaseDN.FromXML
        Throw New NotImplementedException
        '   Return FromXML(Me.GetType, ptr)
    End Function


    Public Overridable Function FromXML(ByVal tipoIXMLAdaptador As System.Type, ByVal ptr As IO.TextReader) As Object

        Dim oxml As IXMLAdaptador
        Dim xmls As New Xml.Serialization.XmlSerializer(tipoIXMLAdaptador)

        oxml = xmls.Deserialize(ptr)
        oxml.XMLAdaptadorToObjeto(Me)
        Return Me

        'Dim xmls As New Xml.Serialization.XmlSerializer(tipoIXMLAdaptador)
        'Dim oxml As IXMLAdaptador
        'Dim memoryStream As New System.IO.MemoryStream(StringToUTF8ByteArray(ptr.ToString))
        'Dim xmlTextWriter As New System.Xml.XmlTextWriter(memoryStream, System.Text.Encoding.UTF8)
        'oxml = xmls.Deserialize(memoryStream)
        'oxml.XMLAdaptadorToObjeto(Me)
        'Return Me

    End Function

    Private Function StringToUTF8ByteArray(ByVal pXmlString As String) As Byte()

        Dim encoding As New System.Text.UTF8Encoding()
        Dim byteArray As Byte() = encoding.GetBytes(pXmlString)
        Return byteArray
    End Function


    Public Overridable Function ToXML() As String Implements IEntidadBaseDN.ToXML
        Throw New NotImplementedException
    End Function
    Public Overridable Function ToXML(ByVal tipoIXMLAdaptador As System.Type) As String

        Dim oxml As IXMLAdaptador = Activator.CreateInstance(tipoIXMLAdaptador)
        Dim xmls As New Xml.Serialization.XmlSerializer(tipoIXMLAdaptador)
        Dim tw As New IO.StringWriter

        oxml.ObjetoToXMLAdaptador(Me)
        xmls.Serialize(tw, oxml)

        Return tw.ToString

        'Dim ms As New System.IO.MemoryStream
        'Dim xmltext As New System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8)

        'Dim oxml As IXMLAdaptador = Activator.CreateInstance(tipoIXMLAdaptador)
        'Dim xmls As New Xml.Serialization.XmlSerializer(tipoIXMLAdaptador)


        'oxml.ObjetoToXMLAdaptador(Me)
        'xmls.Serialize(xmltext, oxml)

        'Return UTF8ByteArrayToString(ms.ToArray)


    End Function



    Private Function UTF8ByteArrayToString(ByVal characters As Byte()) As String

        Dim encoding As New System.Text.UTF8Encoding
        Dim constructedString As String = encoding.GetString(characters)
        Return constructedString
    End Function


    Public Function EsIgualRapido(ByVal pObjeto As Object) As Boolean
        'comparamos otro objeto con éste, si son del mismo tipo y tienen el mismo id, devolvemos true
        Dim miEDN As EntidadBaseDN

        Try
            If pObjeto Is Nothing Then
                Return False
            End If

            If pObjeto.GetType Is Me.GetType Then

                miEDN = CType(pObjeto, EntidadBaseDN)
                If miEDN.ID = Me.ID Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If
        Catch ex As Exception
            Throw
        End Try
    End Function

    Private Function GenerarIDGlobal() As String
        Return System.Guid.NewGuid.ToString()
    End Function

#End Region


    ''' <summary>
    ''' funcion encrgada de verificar si dos objetos representan la misma entidad de negocio
    ''' </summary>
    ''' <param name="pEntidad">la entidad a evaaluar</param>
    ''' <param name="pMismaRef">indica si es el mismo objeto en memoria</param>
    ''' <returns>true si representa a la msima entidad, esta función deberá ser sobre escrita incluyendo los campos clave de la entidad, si no es seguro que estos campos clave solo puedan estar asoociados a un guid</returns>
    ''' <remarks></remarks>
    Public Overridable Function RepresentaMismaEntidad(ByVal pEntidad As IEntidadBaseDN, ByRef pMensaje As String, ByRef pMismaRef As Boolean) As Boolean Implements IEntidadBaseDN.RepresentaMismaEntidad
        If pEntidad Is Nothing Then
            pMensaje = "pEntidad es nulo"
            pMismaRef = False
            Return False
        End If

        If pEntidad Is Me Then
            pMismaRef = True
        Else
            pMismaRef = False
        End If


        If Me.GUID = pEntidad.GUID Then

            Return True
        Else
            pMensaje = "El identificador unico no coincide"
            Return False
        End If

    End Function

    ''' <summary>
    ''' ifunción encargada de verificar si dos entidades representan la lsima entida de negocio
    ''' usara la fucnión RepresentaMismaEntidad del parámetro pEntidad1Comparadora para verificar si son la misma entidad de negocio
    ''' </summary>
    ''' <param name="pEntidad1Comparadora">suministrador de la logica especifica de comparación</param>
    ''' <param name="pEntidad2"></param>
    ''' <param name="pMismaRef"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function RepresentanMismaEntidad(ByVal pEntidad1Comparadora As IEntidadBaseDN, ByRef pMensaje As String, ByVal pEntidad2 As IEntidadBaseDN, ByRef pMismaRef As Boolean) As Boolean


        If pEntidad1Comparadora Is pEntidad2 Then
            pMismaRef = True
            Return True
        End If

        If pEntidad1Comparadora Is Nothing Then
            pMismaRef = False
            Return False

        Else
            Return pEntidad1Comparadora.RepresentaMismaEntidad(pEntidad2, pMensaje, pMismaRef)

        End If


    End Function
    ''' <summary>
    ''' ifunción encargada de verificar si dos entidades representan la lsima entida de negocio
    ''' usara la fucnión RepresentaMismaEntidad del parámetro pEntidad1Comparadora y de pEntidad2Comparadora
    ''' </summary>
    ''' <param name="pEntidad1Comparadora">suministrador de la logica especifica de la primera comparación</param>
    ''' <param name="pEntidad2Comparadora">suministrador de la logica especifica de la segunda comparación</param>
    ''' <param name="pMismaRef"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function RepresentanMismaEntidadBidireccional(ByVal pEntidad1Comparadora As IEntidadBaseDN, ByVal pEntidad2Comparadora As IEntidadBaseDN, ByRef pMensaje As String, ByRef pMismaRef As Boolean) As Boolean

        If pEntidad1Comparadora Is pEntidad2Comparadora Then
            pMismaRef = True
            Return True
        End If
        pMismaRef = False


        If pEntidad1Comparadora Is Nothing Then

            Return False

        Else
            If pEntidad1Comparadora.RepresentaMismaEntidad(pEntidad2Comparadora, pMensaje, pMismaRef) Then
                If pEntidad2Comparadora.RepresentaMismaEntidad(pEntidad1Comparadora, pMensaje, pMismaRef) Then
                    pMismaRef = False
                    Return True
                Else
                    pMismaRef = False
                    Return False
                End If
            Else
                pMismaRef = False
                Return False
            End If

        End If


    End Function

    Public Function Typo() As System.Type Implements IEntidadBaseDN.Typo
        Return Me.GetType()
    End Function


    Public Function ToHtGUIDs(ByVal phtGUIDEntidades As System.Collections.Hashtable, ByRef clones As ColIEntidadDN) As System.Collections.Hashtable Implements IEntidadBaseDN.ToHtGUIDs


        If clones Is Nothing Then
            clones = New ColIEntidadDN
        End If

        If phtGUIDEntidades Is Nothing Then
            phtGUIDEntidades = New System.Collections.Hashtable
        Else
            ' si ya estoy procesado  o procensando no continuo

            If phtGUIDEntidades.ContainsKey(Me.mGUID) Then
                Dim entidad As IEntidadDN = phtGUIDEntidades.Item(Me.mGUID)

                ' si no soy yo es que soy un clon
                If Not entidad Is Me Then

                    If Not clones.Contains(Me) Then
                        clones.Add(Me)
                    End If
                    If Not clones.Contains(entidad) Then
                        clones.Add(entidad)
                    End If
                End If
                Return phtGUIDEntidades
            End If
        End If
        ' me añado ami y luego a mis referecnias

        phtGUIDEntidades.Add(Me.mGUID, Me)


        'For Each entidad As IEntidadDN In Me.mColPartes
        '    entidad.ToHtGUIDs(phtGUIDEntidades, clones)
        'Next

        Return phtGUIDEntidades


    End Function

    Public ReadOnly Property IdentificacionTexto() As Object Implements IEntidadBaseDN.IdentificacionTexto
        Get
            Return Me.mGUID & "/" & Me.mID & "/" & Me.GetType.Name
        End Get
    End Property
End Class



Public Interface IXMLAdaptador
    Sub ObjetoToXMLAdaptador(ByVal pEntidad As IEntidadBaseDN)
    Sub XMLAdaptadorToObjeto(ByVal pEntidad As IEntidadBaseDN)

End Interface