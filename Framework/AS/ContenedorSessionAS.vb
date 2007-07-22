Public Class ContenedorSessionAS
    Inherits Framework.AS.BaseAS


    Private Shared _ContenedorSessionC As New System.Net.CookieContainer



    Public Shared ReadOnly Property contenedorSessionC() As System.Net.CookieContainer

        Get
            Return _ContenedorSessionC
        End Get

    End Property

End Class
