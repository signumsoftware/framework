Imports Despliegue.Compartido
Imports Despliegue.DN

Imports System.IO
Imports System.Collections.Generic

Public Class ComparadorArchivos

    Public Shared Function Diferencias(ByVal pColArchivos As List(Of ArchivoDN), ByVal server As List(Of ArchivoDN)) As List(Of ArchivoOrdenesDN)
        Dim ht As New Dictionary(Of String, ArchivoDN)
        Dim result As New List(Of ArchivoOrdenesDN)

        For Each archcliente As ArchivoDN In pColArchivos
            ht.Add(archcliente.Ruta, archcliente)
        Next

        For Each archserver As ArchivoDN In server
            If ht.ContainsKey(archserver.Ruta) Then

                Dim archcliente As ArchivoDN = ht(archserver.Ruta)

                If (archcliente.Comparable(archserver)) Then
                    If (archcliente.CompareTo(archserver) <> 0) Then
                        result.Add(New ArchivoOrdenesDN(archserver, Orden.Actualizar))
                    End If
                Else
                    result.Add(New ArchivoOrdenesDN(archserver, Orden.Actualizar))
                End If

            Else
                result.Add(New ArchivoOrdenesDN(archserver, Orden.Crear))
            End If
        Next


        Return result
    End Function
End Class
