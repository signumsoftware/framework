Imports System.IO
Imports Despliegue.DN
Imports Despliegue.Compartido



Public Class ClienteAD


    Public Shared Sub EscribirArchivo(ByVal str As Stream, ByVal ruta As String, ByVal evento As DelProgressFile)

        ArchivoAD.CreateDirectoryRecursive(Path.GetDirectoryName(ruta))

        Using fs As New FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None)

            Using unzipped As Stream = ArchivoyZipAD.DameStreamDesZipeante(str)

                ArchivoAD.Copiar(unzipped, fs, evento)

            End Using

        End Using

    End Sub

End Class
