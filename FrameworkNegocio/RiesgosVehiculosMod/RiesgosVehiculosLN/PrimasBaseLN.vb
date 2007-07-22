Public Class PrimasBaseLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN


    Public Overloads Function RecuperarLista() As FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN

        Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN

        colprimas.AddRangeObject(MyBase.RecuperarLista(Of FN.RiesgosVehiculos.DN.PrimaBaseRVDN))

        Return colprimas


    End Function

End Class
