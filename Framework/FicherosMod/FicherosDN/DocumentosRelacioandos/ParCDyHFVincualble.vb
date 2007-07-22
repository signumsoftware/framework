
Public Class ParCDyHFVincualble

    Public CD As Ficheros.FicherosDN.CajonDocumentoDN
    Public HF As Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN

End Class




Public Class ColParCDyHFVincualble
    Inherits List(Of ParCDyHFVincualble)

    Public Function Verificar(ByRef mensaje As String) As Boolean
        ' no debe haber ningun cajon documento repetido


        Dim colcd As New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN

        For Each par As ParCDyHFVincualble In Me

            If colcd.Contiene(par.CD, DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                mensaje = "almenos uno repetido"
                Return False
            End If
        Next


        Return True


    End Function
End Class


