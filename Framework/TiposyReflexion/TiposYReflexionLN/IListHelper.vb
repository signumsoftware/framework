Namespace LN

    Public Class ListHelper(Of t)


        Public Shared Function Convertir(ByVal il As IEnumerable) As Generic.List(Of t)

            Dim lsitaof As New Generic.List(Of t)


            For Each o As Object In il
                lsitaof.Add(CType(o, t))
            Next


            Return lsitaof
        End Function

    End Class

End Namespace
