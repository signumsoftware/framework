Imports Framework.DatosNegocio

Public Interface IDatosTarifaDN
    Inherits IEntidadDN

    Property Tarifa() As TarifaDN
    Property HEEmpresaColaboradora() As FN.Empresas.DN.HEEntidadColaboradoraDN
    Function ClonarDatosTarifa() As IDatosTarifaDN
    Property ValorBonificacion() As Double

End Interface
