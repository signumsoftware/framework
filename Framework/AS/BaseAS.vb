Namespace Framework.AS
    Public Class BaseAS
        Protected mLocalizacionServicioGeneral As String
        Protected mLocalizacionServidorReemplazar() As String
        Protected mLocalizacionServicioEspecifico As String

        Sub New()


            mLocalizacionServicioGeneral = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("LocalizacionServidor")

            Dim textoaReemplazr As String = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("LocalizacionServidorReemplazar")

            If Not String.IsNullOrEmpty(textoaReemplazr) Then
                mLocalizacionServidorReemplazar = textoaReemplazr.Split("*")

            End If

            mLocalizacionServicioEspecifico = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("Lse") ' a esto se le debira adicionar el nombre en concreto del servicio a redireccionar

        End Sub

        Public Function RedireccionURL(ByVal url As String) As String
            Dim str As String = url

            If Not mLocalizacionServidorReemplazar Is Nothing Then

                If Not mLocalizacionServicioGeneral Is Nothing AndAlso mLocalizacionServicioGeneral <> "" Then
                    For Each cadena As String In mLocalizacionServidorReemplazar
                        str = str.Replace(cadena, mLocalizacionServicioGeneral)
                        Debug.WriteLine(str)
                    Next

                    Return str

                End If
            End If

            Debug.WriteLine(url)

            Return url

        End Function

        Protected Function RecuperarDeCache(ByVal clave As String, ByVal cache As Hashtable) As Object
            If cache.ContainsKey(clave) Then
                Return cache.Item(clave)
            Else
                Return Nothing
            End If
        End Function

    End Class

End Namespace

