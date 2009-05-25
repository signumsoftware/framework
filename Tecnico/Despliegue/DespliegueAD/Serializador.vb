Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO


Public Class Serializador

    Public Shared Function Deserializar(ByVal bites As Byte()) As Object

        If bites Is Nothing Then
            Return Nothing
        End If

        Dim sb As New BinaryFormatter

        Using mStream As MemoryStream = New MemoryStream(bites)
            Return sb.Deserialize(mStream)
        End Using

    End Function

    Public Shared Function Serializar(ByVal objeto As Object) As Byte()

        If objeto Is Nothing Then
            Return Nothing
        End If

        Dim sb As New BinaryFormatter

        Using mStream As New MemoryStream
            sb.Serialize(mStream, objeto)
            Return mStream.ToArray()
        End Using

    End Function

End Class