

Public Class Serializador

    Public Shared Function DesSerializar(ByVal bites As Byte()) As Object
        Dim sb As System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
        Dim MStream As System.IO.MemoryStream

        If bites Is Nothing Then
            Return Nothing
        End If


        'empaquetamos los datos 
        sb = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
        MStream = New System.IO.MemoryStream(bites)
        Return sb.Deserialize(MStream)

    End Function

    Public Shared Function Serializar(ByVal objeto As Object) As Byte()
        Dim sb As System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
        Dim MStream As System.IO.MemoryStream

        If objeto Is Nothing Then
            Return Nothing
        End If


        'Desempaquetamos los datos 
        sb = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
        MStream = New System.IO.MemoryStream()
        sb.Serialize(MStream, objeto)


        Return MStream.ToArray

    End Function

    Public Shared Function SerializarXML(ByVal objeto As Object) As String
        Dim serXML As System.Xml.Serialization.XmlSerializer
        Dim sw As System.IO.StringWriter

        If objeto Is Nothing Then
            Return Nothing
        End If

        serXML = New System.Xml.Serialization.XmlSerializer(objeto.GetType())
        sw = New System.IO.StringWriter()
        serXML.Serialize(sw, objeto)

        SerializarXML = sw.ToString()

        sw.Dispose()

    End Function

    Public Shared Function DesSerializarXML(ByVal cadenaXML As String, ByVal tipo As System.Type) As Object
        Dim serXML As System.Xml.Serialization.XmlSerializer
        Dim sr As System.IO.StringReader

        If cadenaXML Is Nothing OrElse cadenaXML = String.Empty Then
            Return Nothing
        End If

        serXML = New System.Xml.Serialization.XmlSerializer(tipo)
        sr = New System.IO.StringReader(cadenaXML)

        DesSerializarXML = serXML.Deserialize(sr)

        sr.Dispose()

    End Function

End Class