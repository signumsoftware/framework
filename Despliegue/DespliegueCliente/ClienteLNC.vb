Imports System.IO '' necesario para el PathCombine y el CreateDirectory
Imports System.Collections.Generic

Imports Despliegue.Compartido
Imports Despliegue.DN


Public Class ClienteLNC



    Public Sub ActualizarTodo(ByVal pdir As String, _
        ByVal estNumArch As DelEstNumArch, _
        ByVal actArchivo As DelActuArchivo, _
        ByVal progressFile As DelProgressFile, _
        ByVal ejecutYMorir As DelEjecutarYMorir)

        Dim rejec As String


        Using ntc As New ClienteAS

            Dim micol As List(Of ArchivoDN) = ArchivoAD.Recuperar(pdir)

            Dim cao As List(Of ArchivoOrdenesDN) = ntc.PedirCambios(micol)

            Dim tot As Int64 = 0
            For Each archOrden As ArchivoOrdenesDN In cao
                tot += archOrden.Archivo.Tam
            Next

            estNumArch(Convert.ToInt32(tot))


            For Each archOrden As ArchivoOrdenesDN In cao

                Dim fileTam As Int32 = Convert.ToInt32(archOrden.Archivo.Tam)
                actArchivo(archOrden.Archivo.Ruta, archOrden.Orden, fileTam)
                ProcesarOrden(ntc, pdir, archOrden, progressFile)

            Next

            rejec = ntc.RutaEjecutable()

        End Using

        ejecutYMorir(rejec)
        

    End Sub

    Private Sub ProcesarOrden(ByVal ntc As ClienteAS, ByVal directorio As String, ByVal pao As ArchivoOrdenesDN, ByVal progressFile As DelProgressFile)

        Dim ruta As String
        ruta = Path.Combine(directorio, pao.Archivo.Ruta)

        If TypeOf pao.Archivo Is ArchivoDirDN Then
            Directory.CreateDirectory(ruta)
        Else

            Using str As Stream = ntc.DameArchivoWeb(pao)
                ClienteAD.EscribirArchivo(str, ruta, progressFile)
            End Using

        End If

    End Sub

End Class