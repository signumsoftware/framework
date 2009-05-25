Imports System.IO
Imports Despliegue.DN
Imports System.IO.Compression


Public Delegate Sub DelProgressFile(ByVal incValor As Integer)

Public Class ArchivoyZipAD


#Region "Metodos Estáticos"

    Public Shared Sub GeneraZipDeArchivo(ByVal archivo As String, ByVal directorioOrig As String, ByVal directorioZip As String)
        Dim rutaOrig As String = Path.Combine(directorioOrig, archivo)
        Dim rutaZip As String = Path.Combine(directorioZip, archivo) & ".zip"
        '' caso el que hay es valido
        If (File.Exists(rutaZip) AndAlso _
            File.GetCreationTime(rutaZip) > File.GetCreationTime(rutaOrig) AndAlso _
            File.GetLastWriteTime(rutaZip) > File.GetLastWriteTime(rutaOrig)) Then Return

        ArchivoAD.CreateDirectoryRecursive(Path.GetDirectoryName(rutaZip))

        Using ofs As FileStream = File.Create(rutaZip)
            Using ozs As New GZipStream(ofs, CompressionMode.Compress)
                Using ifs As FileStream = File.OpenRead(rutaOrig)
                    ArchivoAD.Copiar(ifs, ozs)
                End Using
            End Using
        End Using

    End Sub

   

    Public Shared Function DameStreamDeZip(ByVal archivo As String, ByVal directorioZip As String) As Stream
        Dim ruta As String
        ruta = Path.Combine(directorioZip, archivo) & ".zip"
        Return New FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read)
    End Function

    Public Shared Function DameStreamDesZipeante(ByVal strZipped As Stream) As Stream
        Return New GZipStream(strZipped, CompressionMode.Decompress)
    End Function

#End Region

End Class
