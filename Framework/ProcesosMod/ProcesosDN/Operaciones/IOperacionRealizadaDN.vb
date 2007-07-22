Imports Framework.DatosNegocio


Public Interface IOperacionDN
    Inherits IEntidadDN
    Property VerboOperacion() As VerboDN
    Property ObjetoIndirectoNoModificable() As Boolean

End Interface






Public Interface IOperacionRealizadaDN
    Inherits IOperacionDN
    Property SujetoOperacion() As IEjecutorOperacionDN
    Property FechaOperacion() As DateTime
    Property ObjetoIndirectoOperacion() As IEntidadDN
    Property ObjetoDirectoOperacion() As IEntidadDN
    Property EstadoIOperacionRealizada() As EstadoIOperacionRealizadaDN
    ReadOnly Property RutaSubordinada() As String
    Sub AsignarOIenGrafo(ByVal oi As Object)
End Interface


Public Enum EstadoIOperacionRealizadaDN
    Creada
    Iniciada
    Incidentada
    Anulada
    Terminada

End Enum
Public Enum EstadoActividad
    Creada
    Iniciada
    Cerrada

End Enum