
Public Class ApplicationExceptionAD
    Inherits System.ApplicationException

    Public Sub New(ByVal mensaje As String)
        MyBase.New(mensaje)
    End Sub
    Public Sub New(ByVal mensaje As String, ByVal innerexception As Exception)
        MyBase.New(mensaje)
    End Sub
End Class
