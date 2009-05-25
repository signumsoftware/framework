Imports Microsoft.VisualBasic

Public Class Entorno

    Public Shared DirectorioTrabajo As String
    Public Shared DirectorioTrabajoZip As String
    Public Shared URLArchivos As String
    Public Shared Ejecutable As String


    Shared Sub New()

        CargarConf(DirectorioTrabajo, "DirectorioTrabajo", "D:\arkadel\archserver")

        CargarConf(DirectorioTrabajoZip, "DirectorioTrabajoZip", "D:\arkadel\archserverzip")

        CargarConf(Ejecutable, "Ejecutable", "main.exe")

        CargarConf(URLArchivos, "URLArchivos", "http://localhost/")

    End Sub

    Private Shared Sub CargarConf(ByRef destino As String, ByVal clave As String, ByVal defecto As String)
        destino = ConfigurationManager.AppSettings(clave)
        If (destino Is Nothing) Then
            destino = defecto
        End If
    End Sub

End Class
