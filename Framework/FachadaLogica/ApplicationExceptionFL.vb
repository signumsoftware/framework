Public Class ApplicationExceptionFL
    Inherits System.ApplicationException
    Public Sub New()

    End Sub
    Public Sub New(ByVal mensaje As String, ByVal pEx As System.Exception)
        MyBase.New(mensaje, pEx)

    End Sub
End Class
