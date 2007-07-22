Imports System.Runtime.Serialization

<Serializable()> _
Public Class NingunaFilaAfectadaException
    Inherits DatosNegocioException

    Public Sub New(ByVal mensaje As String)
        MyBase.New(mensaje)
    End Sub

    Public Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.New(info, context)
    End Sub

    Public Overrides Sub GetObjectData(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.GetObjectData(info, context)
    End Sub

End Class
<Serializable()> _
Public Class DatosNegocioException
    Inherits ApplicationException

    Public Sub New(ByVal mensaje As String)
        MyBase.New(mensaje)
    End Sub

    Public Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.New(info, context)
    End Sub

    Public Overrides Sub GetObjectData(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.GetObjectData(info, context)
    End Sub

End Class