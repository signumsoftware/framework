Imports System.IO


<Serializable()> _
Public Class ArchivoHashAD

#Region "Metodos Estáticos"
    ''version de cliente
    Public Shared Function DameHashDeArchivo(ByVal archivo As String, ByVal directorioOrig As String) As Byte()
        Dim rutaOrig As String = Path.Combine(directorioOrig, archivo)

        Dim gh As New GeneradorHashes

        Dim hash As Byte() = gh.CalcularHashFile(rutaOrig)

        Return hash
    End Function

    Public Shared Function DameHashDeArchivo(ByVal archivo As String, ByVal directorioOrig As String, ByVal directorioHash As String) As Byte()
        Dim rutaOrig As String = Path.Combine(directorioOrig, archivo)
        Dim rutaHash As String = Path.Combine(directorioHash, archivo) & ".hash"
        Dim hash As Byte()
        '' caso el que hay es valido
        If (File.Exists(rutaHash) AndAlso _
            File.GetCreationTime(rutaHash) > File.GetCreationTime(rutaOrig) AndAlso _
            File.GetLastWriteTime(rutaHash) > File.GetLastWriteTime(rutaOrig)) Then

            Using fs As FileStream = File.OpenRead(rutaHash)
                ReDim hash(Convert.ToInt32(fs.Length - 1))
                fs.Read(hash, 0, Convert.ToInt32(fs.Length))
            End Using

        Else
            Dim gh As New GeneradorHashes

            hash = gh.CalcularHashFile(rutaOrig)

            Using fs As FileStream = File.Create(rutaHash)
                fs.Write(hash, 0, hash.Length)
            End Using

        End If
        Return hash

    End Function
#End Region



End Class

