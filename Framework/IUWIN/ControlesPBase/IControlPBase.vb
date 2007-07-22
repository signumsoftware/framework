Public Interface IControlPBase

#Region "propiedades"
    'las propiedades que definen el formato y comportamiento estético
    Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP
    'el mensaje de error de validación que muestre el control
    Property MensajeError() As String
    'el texto que aparecerá en el tooltip
    Property ToolTipText() As String
    ReadOnly Property FormularioPadre() As Form
#End Region

End Interface