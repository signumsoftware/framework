Public Class ApplicationExceptionLN

    Inherits System.ApplicationException
    Public Sub New()

    End Sub
    Public Sub New(ByVal mensaje As String)
        MyBase.New(mensaje)
    End Sub
End Class
