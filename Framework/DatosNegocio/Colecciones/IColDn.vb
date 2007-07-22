Public Interface IColDn
    Function AddUnico(ByVal pEntidadDN As IEntidadDN) As ArrayList
    Sub AddRange(ByVal pColeccion As ICollection)
    Function RecuperarItemxHuellaTextual(ByVal huellaTextual As String) As Object
    Function EliminarItemxHuellaTextual(ByVal huellaTextual As String) As Object
    Sub Sort()

    Function RecuperarLsitaGUID() As Generic.List(Of String)
End Interface
