Imports Despliegue.DN
Imports System.IO
Imports System.Collections.Generic


Public Class ArchivoAD

    Public Shared Function Recuperar(ByVal directorio As String, Optional ByVal directorioHash As String = Nothing) As List(Of ArchivoDN)

        If (Not Directory.Exists(directorio)) Then
            ArchivoAD.CreateDirectoryRecursive(directorio)
        End If

        If (Not directorioHash Is Nothing AndAlso Not Directory.Exists(directorioHash)) Then
            ArchivoAD.CreateDirectoryRecursive(directorioHash)
        End If

        Dim pDi As New DirectoryInfo(directorio)

        Dim result As New List(Of ArchivoDN)
        Recuperar(result, pDi, "", directorio, directorioHash)
        Return result

    End Function


    Private Shared Sub Recuperar(ByVal result As List(Of ArchivoDN), ByVal pDi As DirectoryInfo, ByVal acum As String, ByVal directorio As String, ByVal directorioHash As String)

        If (directorioHash <> Nothing) Then
            ArchivoAD.CreateDirectory(Path.Combine(directorioHash, acum))
        End If


        For Each fi As FileInfo In pDi.GetFiles()


            Dim ruta As String = Path.Combine(acum, fi.Name)

            Dim hash As Byte()
            If (directorioHash = Nothing) Then
                hash = ArchivoHashAD.DameHashDeArchivo(ruta, directorio) '' para el cliente
            Else
                hash = ArchivoHashAD.DameHashDeArchivo(ruta, directorio, directorioHash) '' para el servidor
            End If

            result.Add(New ArchivoGenDN(ruta, fi.Length, hash, fi.CreationTime, fi.LastWriteTime, fi.LastAccessTime))
        Next


        For Each di As DirectoryInfo In pDi.GetDirectories()

            Dim nacum As String = Path.Combine(acum, di.Name)

            result.Add(New ArchivoDirDN(nacum, di.CreationTime, di.LastWriteTime, di.LastAccessTime))

            Recuperar(result, di, nacum, directorio, directorioHash)
        Next

    End Sub

    Public Shared Sub CreateDirectoryRecursive(ByVal ruta As String)
        If (Not Directory.Exists(ruta)) Then
            CreateDirectoryRecursive(Path.GetDirectoryName(ruta))
            Directory.CreateDirectory(ruta)
        End If
    End Sub

    Public Shared Sub CreateDirectory(ByVal ruta As String)
        If (Not Directory.Exists(ruta)) Then
            Directory.CreateDirectory(ruta)
        End If
    End Sub

    Public Shared Sub Copiar(ByVal orig As Stream, ByVal dest As Stream)
        Copiar(orig, dest, Nothing)
    End Sub

    Public Shared Sub Copiar(ByVal orig As Stream, ByVal dest As Stream, ByVal mievento As DelProgressFile)
        Dim tam As Int32 = 1024 * 4

        Dim buffer As Byte()
        ReDim buffer(tam)

        Dim leido As Int32
        Do
            leido = orig.Read(buffer, 0, tam)
            dest.Write(buffer, 0, leido)

            If (Not mievento Is Nothing) Then
                mievento.Invoke(leido)
            End If

        Loop While leido <> 0
    End Sub

End Class