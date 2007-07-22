
Imports Framework.DatosNegocio
Public Interface IIntervaloTemporal
    Property FI() As DateTime
    Property FF() As DateTime
    Function Contiene(ByVal pValor As DateTime) As Boolean
    Function SolapadoOContenido(ByVal pIntervalo As IIntervaloTemporal) As IntSolapadosOContenido
    Function BienFormado() As Boolean

End Interface











