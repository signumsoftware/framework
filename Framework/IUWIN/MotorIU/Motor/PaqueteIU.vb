
''' <summary>
''' Esta clase es crossover para todos los elementos de IUWin32
''' y, heredada, será la que determine los parámetros que se deben pasar
''' a cada formulario
''' </summary>
Public MustInherit Class PaqueteIU

    Public Function GenerarPaquete() As Hashtable
        Dim mipaquete As New Hashtable
        mipaquete.Add("Paquete", Me)
        Return mipaquete
    End Function

    Public Function GenerarPaquete(ByVal Paquete As Hashtable) As Hashtable
        If Paquete Is Nothing Then
            Return GenerarPaquete()
        End If
        If Paquete.Contains("Paquete") Then
            Paquete.Remove("Paquete")
        End If
        Paquete.Add("Paquete", Me)
        Return Paquete
    End Function

End Class
