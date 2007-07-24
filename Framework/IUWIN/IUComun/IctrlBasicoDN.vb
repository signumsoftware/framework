
Public Interface IctrlBasicoDN
    Property DN() As Object

    Sub Poblar()
    Sub IUaDNgd()
    Sub DNaIUgd()

    Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN)

End Interface
