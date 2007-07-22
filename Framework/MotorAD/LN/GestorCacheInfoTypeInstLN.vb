#Region "Importaciones"

Imports Framework.TiposYReflexion.DN

#End Region

Namespace LN
    Public Class GestorCacheInfoTypeInstLN

#Region "Metodos"
        'TODO: por implementar el sistema de cache
        Public Shared Function RecuperarMapInstanciacion(ByVal tipo As System.Type) As InfoTypeInstClaseDN
            Dim infoiLN As New InfoTypeInstLN

            Return infoiLN.Generar(tipo, Nothing, "")
        End Function
#End Region

    End Class
End Namespace
