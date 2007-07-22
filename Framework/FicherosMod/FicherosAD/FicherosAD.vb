Imports System.IO


Imports Framework.Ficheros.FicherosDN

Public Class FicherosAD
    'nos pasan un array de bytes y la ruta donde debemos guardarlo, y él
    'guarda los bytes en un fichero binario sobrescribiendo lo que hubiera si
    'ya exístía antes. Además, nos pasan opcionalmente un FileInfo, y si no es nothing establecemos
    'la misma fecha de modificación
    Public Shared Sub GuardarDocumentoDesdeArrayDeBites(ByVal pHuellaFichero As HuellaFicheroAlmacenadoIODN)
        Dim mifs As FileStream = Nothing
        Dim mibinarywr As BinaryWriter = Nothing

        Try
            If pHuellaFichero Is Nothing OrElse pHuellaFichero.Datos Is Nothing Then
                Throw New ApplicationException("El documento que se intenta guardar está vacío o la huella es nothing")
            End If

            'comprobamos si existe la ruta de directorios local
            If Not Directory.Exists(pHuellaFichero.RutaCarpetaContenedora) Then
                Directory.CreateDirectory(pHuellaFichero.RutaCarpetaContenedora)
            End If

            'deserializamos el array de bytes y lo guardamos en el archivo
            'abrimos sobrescribiendo
            mifs = New FileStream(pHuellaFichero.RutaAbsoluta, FileMode.Create)

            'abrimos el binarywriter
            mibinarywr = New BinaryWriter(mifs)

            'escribimos el array de bytes entero
            mibinarywr.Write(pHuellaFichero.Datos)

            'volcamos de memoria al disco
            mibinarywr.Flush()

            If Not mifs Is Nothing Then
                mifs.Close()
            End If

            If Not mibinarywr Is Nothing Then
                mibinarywr.Close()
            End If

        Catch ex As System.IO.IOException
            Debug.WriteLine(ex.Message)

            Dim miex As ApplicationExceptionIOAD

            If ex.Message.Contains("Espacio en disco insuficiente") Then
                miex = New EspacioInsuficienteAD("Espacio en disco insuficiente en:", ex)
                Throw miex
            End If


            If TypeOf ex Is System.IO.DirectoryNotFoundException OrElse ex.Message.Contains("No se ha encontrado") OrElse ex.Message.Contains("Could not find") Then
                miex = New RutaNoEncontradaAD("No se ha encontrado ruta:", ex)
                Throw miex
            End If

            Throw


        Catch ex As System.UnauthorizedAccessException
            Throw

        Catch ex As Exception
            Throw
        Finally
            If Not mifs Is Nothing Then
                mifs.Dispose()
            End If

            If Not mibinarywr Is Nothing Then
                mibinarywr.Close()
            End If
        End Try
    End Sub

    Public Shared Function RecuperarDocAArrayBytes(ByVal pRutaFichero As String) As Byte()
        Dim mifs As FileStream = Nothing
        Dim arbytes As Byte()


        Try
            If pRutaFichero Is Nothing Then
                Throw New ApplicationException(" pRutaFichero no puede ser nothing, ruta no especificada")
            End If

            'comprobamos si existe la ruta de directorios local

            If Not IO.File.Exists(pRutaFichero) Then
                'Directory.CreateDirectory(pRutaFichero)
                Throw New ApplicationExceptionIOAD("el fichero no existe " & pRutaFichero, Nothing)
            End If

            'deserializamos el array de bytes y lo guardamos en el archivo
            'abrimos sobrescribiendo
            'Dim fichero As New IO.File
            'fichero = IO.File.Open(pRutaFichero, FileMode.Open, FileAccess.Read, FileShare.Read)

            mifs = New FileStream(pRutaFichero, FileMode.Open, FileAccess.Read)

            ReDim arbytes(mifs.Length)

            mifs.Read(arbytes, 0, mifs.Length)

            If Not mifs Is Nothing Then
                mifs.Close()
            End If

            Return arbytes

        Catch ex As System.IO.IOException
            Debug.WriteLine(ex.Message)

            Dim miex As ApplicationExceptionIOAD


            If ex.Message.Contains("Espacio en disco insuficiente") Then
                miex = New EspacioInsuficienteAD("Espacio en disco insuficiente en:", ex)
                Throw miex
            End If


            If TypeOf ex Is System.IO.DirectoryNotFoundException OrElse ex.Message.Contains("No se ha encontrado") OrElse ex.Message.Contains("Not found") Then
                miex = New RutaNoEncontradaAD("No se ha encontrado ruta:", ex)
                Throw miex
            End If

            Throw

        Catch ex As Exception
            Throw

        Finally
            If Not mifs Is Nothing Then
                mifs.Dispose()
            End If


        End Try
    End Function

    Public Shared Function CrearHuellaFichero(ByVal pRuta As String, ByVal cargada As Boolean) As HuellaFicheroAlmacenadoIODN

        Dim hf As HuellaFicheroAlmacenadoIODN
        hf = New HuellaFicheroAlmacenadoIODN(New System.IO.FileInfo(pRuta))
        If cargada Then
            hf.Datos = RecuperarDocAArrayBytes(pRuta)
        End If
        Return hf
    End Function
    Public Shared Function SerializaraFichero(ByVal crearDirectorios As Boolean, ByVal ruta As String, ByVal datos As Object)
        ' comprobar la existencia del directorio
        If Not Directory.Exists(IO.Path.GetDirectoryName(ruta)) Then
            If crearDirectorios Then
                Directory.CreateDirectory(IO.Path.GetDirectoryName(ruta))
            Else
                Throw New ApplicationException("El directorio no existe")
            End If

            Debug.WriteLine("creado nuevo directorio")
        End If


        Dim sb As System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
        sb = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter

        Dim fs As System.IO.FileStream
        fs = New System.IO.FileStream(ruta, IO.FileMode.Create)

        Try
            sb.Serialize(fs, datos)
            fs.Flush()
        Catch ex As Exception

            MsgBox(ex.Message)
        Finally
            fs.Dispose()
        End Try



    End Function

    Public Shared Sub EliminarFichero(ByVal pRutaFichero As String)

        IO.File.Delete(pRutaFichero)


    End Sub
    Public Shared Sub MoverFicheroFichero(ByVal pRutaFichero As String, ByVal pRutaDirectorioDestino As String)
        'comprobamos si existe la ruta de directorios local
        If Not Directory.Exists(pRutaDirectorioDestino) Then
            Directory.CreateDirectory(pRutaDirectorioDestino)
        End If

        '  IO.File.Move(pRutaFichero, pRutaDirectorioDestino)

        IO.File.Move(pRutaFichero, pRutaDirectorioDestino & "\" & IO.Path.GetFileName(pRutaFichero))


    End Sub

    Public Shared Function DesSerializarDesdeFichero(ByVal rutaDir As String, ByVal fichero As String) As Object

        ' comprobar la existencia del directorio
        Dim di As DirectoryInfo
        di = Directory.CreateDirectory(rutaDir)

        Dim colfi As IO.FileInfo()
        colfi = di.GetFiles(fichero)

        If colfi.Length > 0 Then

            Dim sb As System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            sb = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter

            Dim fs As System.IO.FileStream
            fs = New System.IO.FileStream(rutaDir & "\" & fichero, IO.FileMode.Open, FileAccess.Read, FileShare.Read)

            Try
                DesSerializarDesdeFichero = sb.Deserialize(fs)
                fs.Flush()

            Catch ex As Exception
                Throw ex
            Finally

                fs.Dispose()
            End Try

        End If



    End Function

End Class

Public Class ApplicationExceptionIOAD
    Inherits System.ApplicationException

    Public Sub New(ByVal mensaje As String, ByVal innerException As System.Exception)
        MyBase.New(mensaje, innerException)
    End Sub

End Class
Public Class EspacioInsuficienteAD
    Inherits ApplicationExceptionIOAD

    Public Sub New(ByVal mensaje As String, ByVal innerException As System.Exception)
        MyBase.New(mensaje, innerException)
    End Sub
End Class
Public Class RutaNoEncontradaAD
    Inherits ApplicationExceptionIOAD

    Public Sub New(ByVal mensaje As String, ByVal innerException As System.Exception)
        MyBase.New(mensaje, innerException)
    End Sub
End Class