Public Interface IValidadorModificable
    'este interface hereda de ivalidador, pero permite que le establezcamos
    'la propiedad validador, como si fuésemos personas normales
    'pq los controles necesitan tener un constructor sin parámetros
    'para poder hacer un initalize correctamente


    Property Validador() As Framework.DatosNegocio.IValidador
    Property MensajeErrorValidacion() As String


End Interface