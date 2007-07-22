Public Class Serializador


    Public Shared Function DesSerializar(ByVal pbjeto As Byte()) As Object

        Return Framework.Utilidades.Serializador.DesSerializar(pbjeto)

    End Function
    Public Shared Function Serializar(ByVal pbjeto As Object) As Byte()
        Return Framework.Utilidades.Serializador.Serializar(pbjeto)

    End Function
End Class
