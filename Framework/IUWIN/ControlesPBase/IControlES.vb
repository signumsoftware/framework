Public Interface IControlES
    Inherits IControlPBase
    Inherits ivalidadormodificable

#Region "propiedades"
    Property Formateador() As AuxIU.IFormateador 'formaeador
#End Region

#Region "validación"
    'evento de error de validación
    Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs)

    Event Validado(ByVal sender As Object, ByVal e As EventArgs)

    Sub ErrorValidando(ByVal mensaje As String)
    'este sub es el que desencadena el error de validación.
    'lo declaramos público para que pueda ser provocado desde
    'fuera

    'función de validación
    Sub OnValidating(ByVal e As System.ComponentModel.CancelEventArgs)

#End Region

End Interface