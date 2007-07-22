Public Class AsignarCol

    Public Shared Sub AsignarCol(ByRef ColOriginal As Framework.DatosNegocio.ArrayListValidable, ByRef ColNueva As Framework.DatosNegocio.ArrayListValidable)
        Dim ob As Object
        Dim micol As ArrayList

        Try
            'si es la misma, la devolvemos y salimos
            If ColOriginal Is ColNueva Then
                Exit Sub
            End If

            'si el original no es nada, asignamos directamente la nueva
            If ColOriginal Is Nothing Then
                ColOriginal = ColNueva
                Exit Sub
            End If

            'si no es nada, devolvemos nada
            If ColNueva Is Nothing Then
                ColOriginal = Nothing
                Exit Sub
            End If


            'si la nueva no tiene elementos, borramos los del original
            If ColNueva.Count = 0 Then
                ColOriginal.Clear()
                Exit Sub
            End If

            'llegados a este punto, comprobamos los elementos de la nueva con la original
            For Each ob In ColNueva
                If Not ColOriginal.Contains(ob) Then
                    ColOriginal.Add(ob)
                End If
            Next

            'ahora comprobamos que no se hubiese borrado algún objeto
            micol = New ArrayList

            For Each ob In ColOriginal
                If Not ColNueva.Contains(ob) Then
                    micol.Add(ob)
                End If
            Next

            If micol.Count <> 0 Then
                For Each ob In micol
                    ColOriginal.Remove(ob)
                Next
            End If

        Catch ex As Exception
            Throw (ex)
        End Try
    End Sub

End Class