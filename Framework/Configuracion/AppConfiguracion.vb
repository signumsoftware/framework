Namespace Framework.Configuracion
    Public Class AppConfiguracion
        Private Shared _DatosConfig As Hashtable
        Shared Sub New()
            _DatosConfig = New Hashtable
        End Sub

        Public Shared ReadOnly Property DatosConfig() As Hashtable
            Get
                Return _DatosConfig
            End Get
        End Property

    End Class

End Namespace

